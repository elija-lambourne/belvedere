using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using belvedere.Core.Util;
using Amazon.S3;
using Amazon.S3.Model;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Directory = MetadataExtractor.Directory;

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

    public ValueTask<FileMetadata> SaveFileAsync(IFormFile file, string? title, string? description);

    /// <summary>
    /// Saves a byte array as a file to storage and returns metadata.
    /// </summary>
    public ValueTask<FileMetadata> SaveBytesAsync(byte[] data, string fileName, string contentType, string? title, string? description);

    public sealed record FileMetadata(
        string StorageKey,
        string FileName,
        long FileSize,
        string MimeType,
        int Width,
        int Height,
        DateTime CreatedAt,
        DateTime CapturedAt,
        string? Make,
        string? Model,
        double? ExposureTime,
        double? FNumber,
        int? Iso,
        double? Latitude,
        double? Longitude,
        bool IsLivePhoto);
}

/// <summary>
/// Implementation of cloud storage operations using AWS S3 SDK.
/// </summary>
/// <remarks>
/// Uses Amazon.S3 SDK for presigned URL generation and file uploads.
/// Compatible with AWS S3 and S3-compatible services like MinIO and Garage.
/// </remarks>
internal sealed class S3StorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IOptions<StorageSettings> settings, ILogger<S3StorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _s3Client = CreateS3Client();
    }

    /// <summary>
    /// Creates and configures an S3 client based on StorageSettings.
    /// </summary>
    /// <returns>A configured IAmazonS3 client instance.</returns>
    /// <remarks>
    /// Configures the client with custom endpoint URL if provided (for S3-compatible services).
    /// Uses the configured access key and secret key from settings.
    /// </remarks>
    private IAmazonS3 CreateS3Client()
    {
        string accessKey = _settings.AccessKey ?? throw new InvalidOperationException("Storage:AccessKey must be configured");
        string secretKey = _settings.SecretKey ?? throw new InvalidOperationException("Storage:SecretKey must be configured");

        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        
        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_settings.Region ?? "us-east-1"),
            ForcePathStyle = _settings.ForcePathStyle
        };

        // If a custom service URL is provided (for S3-compatible services), configure it
        if (!string.IsNullOrEmpty(_settings.ServiceUrl))
        {
            s3Config.ServiceURL = _settings.ServiceUrl;
        }

        return new AmazonS3Client(credentials, s3Config);
    }

    /// <summary>
    /// Generates a presigned URL for accessing a stored object in S3.
    /// </summary>
    /// <param name="storageKey">The key/path of the object in S3 bucket (e.g., "photos/image123.jpg").</param>
    /// <param name="expires">The duration for which the URL remains valid.</param>
    /// <returns>A complete presigned URL that grants temporary access to the object.</returns>
    /// <remarks>
    /// The presigned URL includes AWS Signature Version 4 signature and is valid for the specified duration.
    /// After expiration, the URL becomes invalid and cannot be used to access the object.
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

        string bucketName = _settings.BucketName ?? throw new InvalidOperationException("Storage:BucketName must be configured");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.AddSeconds(expires.TotalSeconds),
            Verb = HttpVerb.GET
        };

        string url = _s3Client.GetPreSignedURL(request);
        _logger.LogDebug("Generated presigned URL for key {StorageKey}", storageKey);

        return ValueTask.FromResult(url);
    }

    /// <summary>
    /// Saves a file to S3 storage.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="title">Optional title/metadata for the file.</param>
    /// <param name="description">Optional description/metadata for the file.</param>
    /// <returns>FileMetadata containing information about the uploaded file.</returns>
    /// <remarks>
    /// Uploads the file to S3 using the bucket configured in StorageSettings.
    /// The storage key is derived from the file name.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when file is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required storage settings are not configured.</exception>
    public async ValueTask<IStorageService.FileMetadata> SaveFileAsync(IFormFile file, string? title, string? description)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        string bucketName = _settings.BucketName ?? throw new InvalidOperationException("Storage:BucketName must be configured");

        string safeFileName = Path.GetFileName(file.FileName);
        string extension = Path.GetExtension(safeFileName);
        string storageKey = $"photos/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        string mimeType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        DateTime createdAt = DateTime.UtcNow;

        try
        {
            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var extracted = ExtractMetadata(memoryStream, createdAt);

            memoryStream.Position = 0;
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = storageKey,
                InputStream = memoryStream,
                ContentType = mimeType
            };

            if (!string.IsNullOrWhiteSpace(title))
            {
                putRequest.Metadata["title"] = title;
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                putRequest.Metadata["description"] = description;
            }

            await _s3Client.PutObjectAsync(putRequest);
            _logger.LogInformation("File uploaded successfully to S3. StorageKey: {StorageKey}, Size: {FileSize}", storageKey, file.Length);

            return new IStorageService.FileMetadata(
                StorageKey: storageKey,
                FileName: safeFileName,
                FileSize: file.Length,
                MimeType: mimeType,
                Width: extracted.Width,
                Height: extracted.Height,
                CreatedAt: createdAt,
                CapturedAt: extracted.CapturedAt,
                Make: extracted.Make,
                Model: extracted.Model,
                ExposureTime: extracted.ExposureTime,
                FNumber: extracted.FNumber,
                Iso: extracted.Iso,
                Latitude: extracted.Latitude,
                Longitude: extracted.Longitude,
                IsLivePhoto: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3. FileName: {FileName}, StorageKey: {StorageKey}", file.FileName, storageKey);
            throw;
        }
    }

    public async ValueTask<IStorageService.FileMetadata> SaveBytesAsync(byte[] data, string fileName, string contentType, string? title, string? description)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required", nameof(fileName));

        string bucketName = _settings.BucketName ?? throw new InvalidOperationException("Storage:BucketName must be configured");

        string extension = Path.GetExtension(fileName);
        string storageKey = $"photos/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

        try
        {
            await using var ms = new MemoryStream(data);
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = storageKey,
                InputStream = ms,
                ContentType = contentType
            };

            if (!string.IsNullOrWhiteSpace(title)) putRequest.Metadata["title"] = title;
            if (!string.IsNullOrWhiteSpace(description)) putRequest.Metadata["description"] = description;

            await _s3Client.PutObjectAsync(putRequest);
            _logger.LogInformation("Bytes uploaded successfully to S3. StorageKey: {StorageKey}, Size: {FileSize}", storageKey, data.Length);

            return new IStorageService.FileMetadata(
                StorageKey: storageKey,
                FileName: fileName,
                FileSize: data.LongLength,
                MimeType: contentType,
                Width: 0,
                Height: 0,
                CreatedAt: DateTime.UtcNow,
                CapturedAt: DateTime.UtcNow,
                Make: null,
                Model: null,
                ExposureTime: null,
                FNumber: null,
                Iso: null,
                Latitude: null,
                Longitude: null,
                IsLivePhoto: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload bytes to S3. FileName: {FileName}, StorageKey: {StorageKey}", fileName, storageKey);
            throw;
        }
    }

    private static ExtractedImageMetadata ExtractMetadata(Stream imageStream, DateTime fallbackCapturedAt)
    {
        imageStream.Position = 0;

        IReadOnlyList<Directory> directories = ImageMetadataReader.ReadMetadata(imageStream).ToList();

        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
        var jpeg = directories.OfType<JpegDirectory>().FirstOrDefault();

        int width = TryGetInt(jpeg, JpegDirectory.TagImageWidth)
                    ?? TryGetInt(subIfd, ExifDirectoryBase.TagExifImageWidth)
                    ?? TryGetInt(ifd0, ExifDirectoryBase.TagImageWidth)
                    ?? 0;

        int height = TryGetInt(jpeg, JpegDirectory.TagImageHeight)
                     ?? TryGetInt(subIfd, ExifDirectoryBase.TagExifImageHeight)
                     ?? TryGetInt(ifd0, ExifDirectoryBase.TagImageHeight)
                     ?? 0;

        DateTime capturedAt = TryGetDateTime(subIfd, ExifDirectoryBase.TagDateTimeOriginal)
                              ?? TryGetDateTime(subIfd, ExifDirectoryBase.TagDateTimeDigitized)
                              ?? TryGetDateTime(ifd0, ExifDirectoryBase.TagDateTime)
                              ?? fallbackCapturedAt;

        double? latitude = null;
        double? longitude = null;
        GeoLocation? location = gps?.GetGeoLocation();
        if (location.HasValue)
        {
            latitude = location.Value.Latitude;
            longitude = location.Value.Longitude;
        }

        return new ExtractedImageMetadata(
            Width: width,
            Height: height,
            CapturedAt: capturedAt,
            Make: ifd0?.GetDescription(ExifDirectoryBase.TagMake),
            Model: ifd0?.GetDescription(ExifDirectoryBase.TagModel),
            ExposureTime: TryGetRational(subIfd, ExifDirectoryBase.TagExposureTime),
            FNumber: TryGetRational(subIfd, ExifDirectoryBase.TagFNumber),
            Iso: TryGetInt(subIfd, ExifDirectoryBase.TagIsoEquivalent),
            Latitude: latitude,
            Longitude: longitude);
    }

    private static int? TryGetInt(Directory? directory, int tag)
    {
        if (directory is null)
        {
            return null;
        }

        return directory.TryGetInt32(tag, out int value) ? value : null;
    }

    private static double? TryGetRational(Directory? directory, int tag)
    {
        if (directory is null)
        {
            return null;
        }

        return directory.TryGetRational(tag, out Rational value) ? value.ToDouble() : null;
    }

    private static DateTime? TryGetDateTime(Directory? directory, int tag)
    {
        if (directory is null)
        {
            return null;
        }

        return directory.TryGetDateTime(tag, out DateTime value) ? value : null;
    }

    private sealed record ExtractedImageMetadata(
        int Width,
        int Height,
        DateTime CapturedAt,
        string? Make,
        string? Model,
        double? ExposureTime,
        double? FNumber,
        int? Iso,
        double? Latitude,
        double? Longitude);
}


