namespace UrlShortner.Contracts;

public sealed record ShortenUrlResponse
{
    public required string Code { get; init; }

    public required string ShortUrl { get; init; }
}
