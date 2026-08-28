namespace UrlShortner.Contracts;

public sealed record ErrorResponse
{
    public required IReadOnlyList<string> Errors { get; init; }

    public string? CorrelationId { get; init; }
}
