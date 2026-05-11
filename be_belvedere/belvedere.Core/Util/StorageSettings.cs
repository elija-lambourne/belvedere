namespace belvedere.Core.Util;

public sealed class StorageSettings
{
    public const string SectionKey = "Storage";

    public string? ServiceUrl { get; init; }
    public string? BucketName { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? Region { get; init; }
    public bool ForcePathStyle { get; init; } = true;
}

