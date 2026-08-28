namespace UrlShortner.Entities;

/// <summary>
/// The stored short link. <see cref="Code"/> is the Cassandra partition key,
/// which is what lets the store claim it with one conditional insert.
/// </summary>
public sealed class ShortenedUrl
{
    public Guid Id { get; init; }

    public string LongUrl { get; init; } = string.Empty;

    public string ShortUrl { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }
}
