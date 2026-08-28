namespace UrlShortner.Contracts;

public sealed record ShortenUrlRequest
{
    public string Url { get; init; } = string.Empty;
}
