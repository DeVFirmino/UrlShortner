namespace UrlShortner.Contracts;

public sealed record ServerIdentityResponse
{
    public required string Server { get; init; }
}
