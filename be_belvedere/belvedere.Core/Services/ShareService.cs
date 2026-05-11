using System.Security.Cryptography;
using System.Text;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace belvedere.Core.Services;

/// <summary>
/// Service interface for managing shared access to photos and albums.
/// </summary>
/// <remarks>
/// Provides methods for creating shareable links and validating access to shared resources.
/// Share links can be password-protected and time-limited.
/// </remarks>
public interface IShareService
{
    /// <summary>
    /// Creates a new share link for either an album or a photo.
    /// </summary>
    /// <param name="albumId">The ID of the album to share (null if sharing a photo).</param>
    /// <param name="photoId">The ID of the photo to share (null if sharing an album).</param>
    /// <param name="password">Optional password to protect the shared link.</param>
    /// <param name="expiresAt">Optional expiration date/time for the share link.</param>
    /// <returns>The created <see cref="ShareKey"/> with generated key, hash, and metadata.</returns>
    /// <remarks>
    /// Exactly one of albumId or photoId must be provided; providing both or neither results in an exception.
    /// The generated share key uses cryptographically secure randomness and URL-safe base64 encoding.
    /// Passwords are hashed using PBKDF2-SHA256 before storage.
    /// If expiration is provided, it must be in the future.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when neither or both albumId and photoId are provided.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expiresAt is not in the future.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to generate a unique share key after max attempts.</exception>
    public ValueTask<ShareKey> CreateShareAsync(Guid? albumId, Guid? photoId, string? password, DateTime? expiresAt);

    /// <summary>
    /// Resolves and validates a share link, checking password protection and expiration.
    /// </summary>
    /// <param name="key">The share key to validate.</param>
    /// <param name="password">Optional password for password-protected shares.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing:
    /// - The <see cref="ShareKey"/> if access is granted
    /// - NotFound if the key doesn't exist
    /// - ShareUnauthorized if the password is incorrect or missing for protected shares
    /// - Expired if the share link has expired
    /// </returns>
    /// <remarks>
    /// Performs validation in order: existence, expiration, password validation.
    /// Password comparison uses constant-time comparison to prevent timing attacks.
    /// </remarks>
    public ValueTask<OneOf<ShareKey, NotFound, ShareUnauthorized, Expired>> ResolveShareAsync(string key, string? password = null);

    /// <summary>
    /// Indicates that a share link has expired.
    /// </summary>
    public readonly record struct Expired;

    /// <summary>
    /// Indicates that a share access attempt is unauthorized (e.g., wrong password).
    /// </summary>
    public readonly record struct ShareUnauthorized;
}


/// <summary>
/// Implementation of share service for creating and resolving shared access links.
/// </summary>
/// <remarks>
/// Manages secure share key generation, password hashing, and access validation.
/// Uses PBKDF2-SHA256 for password hashing with configurable iterations and salt length.
/// </remarks>
internal sealed class ShareService(IUnitOfWork uow, ILogger<ShareService> logger) : IShareService
{
    /// <summary>The number of random bytes used to generate each share token.</summary>
    private const int TokenByteLength = 12;

    /// <summary>Maximum number of token generation attempts before failing.</summary>
    private const int MaxTokenAttempts = 8;

    /// <summary>Number of PBKDF2 iterations for password hashing (100,000 per OWASP recommendations).</summary>
    private const int PasswordIterations = 100_000;

    /// <summary>Length of the random salt for password hashing in bytes.</summary>
    private const int SaltLength = 16;

    /// <summary>Length of the derived password hash in bytes.</summary>
    private const int SubkeyLength = 32;

    /// <summary>
    /// Creates a new share link for either an album or a photo.
    /// </summary>
    /// <param name="albumId">The ID of the album to share (null if sharing a photo).</param>
    /// <param name="photoId">The ID of the photo to share (null if sharing an album).</param>
    /// <param name="password">Optional password to protect the shared link.</param>
    /// <param name="expiresAt">Optional expiration date/time for the share link.</param>
    /// <returns>The created <see cref="ShareKey"/> with generated key, hash, and metadata.</returns>
    /// <remarks>
    /// Generates a cryptographically secure random share key and attempts to store it.
    /// If a collision occurs (unlikely but possible), retries up to MaxTokenAttempts times.
    /// Logs warnings for token collisions and errors for critical failures.
    /// </remarks>
    public async ValueTask<ShareKey> CreateShareAsync(Guid? albumId, Guid? photoId, string? password, DateTime? expiresAt)
    {
        ValidateTarget(albumId, photoId);

        if (expiresAt is not null && expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiration must be in the future");
        }

        for (var attempt = 0; attempt < MaxTokenAttempts; attempt++)
        {
            var key = GenerateToken();
            if (await uow.ShareKeyRepository.GetShareKeyByKeyAsync(key) is not null)
            {
                continue;
            }

            var shareKey = new ShareKey
            {
                AlbumId = albumId,
                PhotoId = photoId,
                Key = key,
                PasswordHash = string.IsNullOrWhiteSpace(password) ? null : HashPassword(password),
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            uow.ShareKeyRepository.AddShareKey(shareKey);

            try
            {
                await uow.SaveChangesAsync();
                return shareKey;
            }
            catch (DbUpdateException ex) when (attempt < MaxTokenAttempts - 1)
            {
                logger.LogWarning(ex, "Token collision while creating share key, retrying");
            }
        }

        throw new InvalidOperationException("Unable to generate a unique share key");
    }

    /// <summary>
    /// Resolves and validates a share link, checking password protection and expiration.
    /// </summary>
    /// <param name="key">The share key to validate.</param>
    /// <param name="password">Optional password for password-protected shares.</param>
    /// <returns>
    /// A OneOf containing the ShareKey if access is granted, or an error indicator.
    /// </returns>
    /// <remarks>
    /// Validates in order: key existence, expiration status, password protection.
    /// Uses a discriminated union to safely represent all possible outcomes.
    /// </remarks>
    public async ValueTask<OneOf<ShareKey, NotFound, IShareService.ShareUnauthorized, IShareService.Expired>> ResolveShareAsync(string key, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new NotFound();
        }

        var shareKey = await uow.ShareKeyRepository.GetShareKeyByKeyAsync(key);
        if (shareKey is null)
        {
            return new NotFound();
        }

        if (shareKey.ExpiresAt is not null && shareKey.ExpiresAt <= DateTime.UtcNow)
        {
            return new IShareService.Expired();
        }

        if (!string.IsNullOrWhiteSpace(shareKey.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(password) || !VerifyPassword(password, shareKey.PasswordHash))
            {
                return new IShareService.ShareUnauthorized();
            }
        }

        return shareKey;
    }

    /// <summary>
    /// Validates that exactly one of albumId or photoId is provided.
    /// </summary>
    /// <param name="albumId">The album ID (should be null if photoId is provided).</param>
    /// <param name="photoId">The photo ID (should be null if albumId is provided).</param>
    /// <remarks>
    /// Throws an ArgumentException if both or neither of the IDs are provided.
    /// This ensures that a share link targets exactly one resource.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when both IDs are null or both are not null.</exception>
    private static void ValidateTarget(Guid? albumId, Guid? photoId)
    {
        if ((albumId is null && photoId is null) || (albumId is not null && photoId is not null))
        {
            throw new ArgumentException("Exactly one share target must be supplied");
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random token for a share key.
    /// </summary>
    /// <returns>A URL-safe base64 encoded token.</returns>
    /// <remarks>
    /// Uses RandomNumberGenerator to fill a buffer with random bytes, then encodes
    /// the result using URL-safe base64 encoding (replacing + with -, / with _).
    /// </remarks>
    private static string GenerateToken()
    {
        Span<byte> buffer = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(buffer);
        return Base64UrlEncode(buffer);
    }

    /// <summary>
    /// Encodes bytes to URL-safe base64 format.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>URL-safe base64 string without padding.</returns>
    /// <remarks>
    /// Converts standard base64 by replacing '+' with '-', '/' with '_', and removing '=' padding.
    /// This format is compatible with RFC 4648 base64url encoding.
    /// </remarks>
    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        string base64 = Convert.ToBase64String(bytes);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// Hashes a password using PBKDF2-SHA256 with random salt.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>A formatted hash string containing algorithm, iterations, salt, and hash.</returns>
    /// <remarks>
    /// Uses 100,000 iterations of PBKDF2-SHA256 with a 16-byte random salt.
    /// Returns format: "pbkdf2_sha256$iterations$base64salt$base64hash"
    /// This format is compatible with Django's password hashing format.
    /// </remarks>
    private static string HashPassword(string password)
    {
        Span<byte> salt = stackalloc byte[SaltLength];
        RandomNumberGenerator.Fill(salt);

        byte[] derived = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt.ToArray(),
            PasswordIterations,
            HashAlgorithmName.SHA256,
            SubkeyLength);

        return $"pbkdf2_sha256${PasswordIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(derived)}";
    }

    /// <summary>
    /// Verifies a password against its hash using constant-time comparison.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="encodedHash">The hash string in the format produced by HashPassword.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    /// <remarks>
    /// Parses the encoded hash to extract algorithm, iterations, salt, and expected hash.
    /// Uses CryptographicOperations.FixedTimeEquals to prevent timing attacks.
    /// Returns false if the hash format is invalid or algorithm is unsupported.
    /// </remarks>
    private static bool VerifyPassword(string password, string encodedHash)
    {
        string[] parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || parts[0] != "pbkdf2_sha256")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out int iterations))
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] expectedSubkey = Convert.FromBase64String(parts[3]);

        byte[] actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}


