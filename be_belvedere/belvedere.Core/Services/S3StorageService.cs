using belvedere.Core.Util;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace belvedere.Core.Services;

/// <summary>
/// Service interface for cloud storage operations.
/// </summary>
/// <remarks>
/// Abstracts cloud storage functionality, allowing for different implementations of storage services.
/// </remarks>
public interface IStorageService
{
    /// <summary>
    /// Generates a presigned URL for accessing a stored object with temporary access.
    /// </summary>
    /// <param name="storageKey">The key/path of the object in storage.</param>
    /// <param name="expires">The duration for which the URL remains valid.</param>
    /// <returns>A presigned URL that grants temporary access to the storage object.</returns>
    /// <remarks>
    /// Presigned URLs allow temporary, unauthenticated access to cloud storage objects
    /// without exposing permanent credentials. After expiration, the URL becomes invalid.
    /// </remarks>
    public ValueTask<string> GetPresignedUrlAsync(string storageKey, TimeSpan expires);
}

/// <summary>
/// Implementation of presigned URL generation for AWS S3-compatible storage services.
/// </summary>
/// <remarks>
/// Implements AWS Signature Version 4 signing process to generate presigned URLs.
/// Compatible with AWS S3 and S3-compatible services like MinIO.
/// </remarks>
internal sealed class S3StorageService(IOptions<StorageSettings> settings, ILogger<S3StorageService> logger) : IStorageService
{
    /// <summary>
    /// Generates a presigned URL for accessing a stored object in S3.
    /// </summary>
    /// <param name="storageKey">The key/path of the object in S3 bucket (e.g., "photos/image123.jpg").</param>
    /// <param name="expires">The duration for which the URL remains valid.</param>
    /// <returns>A complete presigned URL with AWS signature that grants temporary access to the object.</returns>
    /// <remarks>
    /// Uses AWS Signature Version 4 to sign the request. The generated URL includes:
    /// - Access credentials scoped to the S3 service and specific region
    /// - Expiration timestamp
    /// - Cryptographic signature preventing tampering
    /// 
    /// The URL format depends on the storage configuration:
    /// - Path-style: https://s3.example.com/bucket/key
    /// - Virtual-hosted-style: https://bucket.s3.example.com/key
    /// 
    /// Configuration is read from StorageSettings including service URL, bucket name,
    /// access key, secret key, and region.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when storageKey is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expires is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required storage settings are not configured.</exception>
    public ValueTask<string> GetPresignedUrlAsync(string storageKey, TimeSpan expires)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required", nameof(storageKey));
        }

        if (expires <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expires), "Expiration must be positive");
        }

        StorageSettings storageSettings = settings.Value;
        string serviceUrl = storageSettings.ServiceUrl ?? throw new InvalidOperationException("Storage:ServiceUrl must be configured");
        string bucketName = storageSettings.BucketName ?? throw new InvalidOperationException("Storage:BucketName must be configured");
        string accessKey = storageSettings.AccessKey ?? throw new InvalidOperationException("Storage:AccessKey must be configured");
        string secretKey = storageSettings.SecretKey ?? throw new InvalidOperationException("Storage:SecretKey must be configured");
        string region = string.IsNullOrWhiteSpace(storageSettings.Region) ? "us-east-1" : storageSettings.Region;

        Uri baseUri = new(serviceUrl, UriKind.Absolute);
        string host = baseUri.Host;
        int? port = baseUri.IsDefaultPort ? null : baseUri.Port;

        string canonicalUri = storageSettings.ForcePathStyle
            ? $"/{EscapeSegment(bucketName)}/{BuildEscapedPath(storageKey)}"
            : $"/{BuildEscapedPath(storageKey)}";

        string requestHost = storageSettings.ForcePathStyle
            ? BuildHost(host, port)
            : BuildHost($"{bucketName}.{host}", port);

        string amzDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string credentialScope = $"{dateStamp}/{region}/s3/aws4_request";

        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = Uri.EscapeDataString($"{accessKey}/{credentialScope}"),
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = ((int)expires.TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host"
        };

        string canonicalQueryString = string.Join("&", query.Select(pair => $"{pair.Key}={pair.Value}"));
        string canonicalHeaders = $"host:{requestHost}\n";
        const string signedHeaders = "host";
        const string payloadHash = "UNSIGNED-PAYLOAD";

        string canonicalRequest = string.Join("\n",
            "GET",
            canonicalUri,
            canonicalQueryString,
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        string stringToSign = string.Join("\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            HashHex(canonicalRequest));

        byte[] signingKey = GetSigningKey(secretKey, dateStamp, region, "s3");
        string signature = ToHex(HmacSha256(signingKey, stringToSign));

        string finalQuery = string.Join("&", query.Select(pair => $"{pair.Key}={pair.Value}")) + $"&X-Amz-Signature={signature}";
        string finalBase = $"{baseUri.Scheme}://{requestHost}";

        string url = $"{finalBase}{canonicalUri}?{finalQuery}";
        logger.LogDebug("Generated presigned URL for key {StorageKey}", storageKey);

        return ValueTask.FromResult(url);
    }

    /// <summary>
    /// Constructs a host string from hostname and optional port number.
    /// </summary>
    /// <param name="host">The hostname.</param>
    /// <param name="port">The optional port number.</param>
    /// <returns>A host string in the format "host" or "host:port".</returns>
    /// <remarks>
    /// If port is null, only the hostname is used. Otherwise, the port is appended with a colon separator.
    /// </remarks>
    private static string BuildHost(string host, int? port) => port is null ? host : $"{host}:{port}";

    /// <summary>
    /// Constructs a URL path with properly URI-escaped segments.
    /// </summary>
    /// <param name="key">The storage key/path (e.g., "folder/subfolder/file").</param>
    /// <returns>A properly escaped URL path.</returns>
    /// <remarks>
    /// Splits the key by '/' and escapes each segment individually to ensure proper URL encoding
    /// of special characters while preserving the path structure.
    /// </remarks>
    private static string BuildEscapedPath(string key) => string.Join('/', key.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(EscapeSegment));

    /// <summary>
    /// URI-escapes a single path segment.
    /// </summary>
    /// <param name="segment">The path segment to escape.</param>
    /// <returns>The URI-escaped segment.</returns>
    /// <remarks>
    /// Uses library standard URI data string escaping to ensure special characters are properly encoded.
    /// </remarks>
    private static string EscapeSegment(string segment) => Uri.EscapeDataString(segment);

    /// <summary>
    /// Derives the AWS Signature Version 4 signing key from the secret access key.
    /// </summary>
    /// <param name="secretKey">The AWS secret access key.</param>
    /// <param name="dateStamp">The date stamp in yyyyMMdd format.</param>
    /// <param name="regionName">The AWS region name (e.g., "us-east-1").</param>
    /// <param name="serviceName">The service name, typically "s3".</param>
    /// <returns>The derived signing key as bytes.</returns>
    /// <remarks>
    /// Implements the HMAC-SHA256 key derivation process required for AWS Signature Version 4.
    /// The process applies HMAC-SHA256 iteratively:
    /// 1. kSecret = HMAC-SHA256("AWS4" + secretKey, dateStamp)
    /// 2. kRegion = HMAC-SHA256(kSecret, regionName)
    /// 3. kService = HMAC-SHA256(kRegion, serviceName)
    /// 4. kSigning = HMAC-SHA256(kService, "aws4_request")
    /// </remarks>
    private static byte[] GetSigningKey(string secretKey, string dateStamp, string regionName, string serviceName)
    {
        byte[] kSecret = Encoding.UTF8.GetBytes($"AWS4{secretKey}");
        byte[] kDate = HmacSha256(kSecret, dateStamp);
        byte[] kRegion = HmacSha256(kDate, regionName);
        byte[] kService = HmacSha256(kRegion, serviceName);
        return HmacSha256(kService, "aws4_request");
    }

    /// <summary>
    /// Computes HMAC-SHA256 hash of data using the specified key.
    /// </summary>
    /// <param name="key">The HMAC key as bytes.</param>
    /// <param name="data">The data to hash as a string.</param>
    /// <returns>The computed HMAC-SHA256 hash as bytes.</returns>
    /// <remarks>
    /// Uses the standard HMACSHA256 algorithm from System.Security.Cryptography.
    /// The data is encoded to UTF-8 before hashing.
    /// </remarks>
    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// Computes SHA256 hash of input string and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>The SHA256 hash as a lowercase hexadecimal string.</returns>
    /// <remarks>
    /// Encodes the input as UTF-8 before hashing, then converts the result to uppercase hex format
    /// and converts to lowercase for consistency with AWS requirements.
    /// </remarks>
    private static string HashHex(string input) => ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    /// <summary>
    /// Converts a byte array to a lowercase hexadecimal string.
    /// </summary>
    /// <param name="data">The bytes to convert.</param>
    /// <returns>A lowercase hexadecimal representation of the bytes.</returns>
    /// <remarks>
    /// Uses the built-in ToHexString method and converts the result to lowercase.
    /// </remarks>
    private static string ToHex(byte[] data) => Convert.ToHexString(data).ToLowerInvariant();
}


