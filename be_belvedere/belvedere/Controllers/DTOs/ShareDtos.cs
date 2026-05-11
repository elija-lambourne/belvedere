namespace belvedere.Controllers.DTOs;

/// <summary>
/// Response model for resolving a share key to its target resource.
/// </summary>
public sealed record ShareResolutionResponse
{
    /// <summary>
    /// The type of the shared resource ("photo" or "album").
    /// </summary>
    public required ResourceType TargetType { get; init; }

    /// <summary>
    /// The unique identifier of the shared resource (photo or album).
    /// </summary>
    public required Guid TargetId { get; init; }
}
