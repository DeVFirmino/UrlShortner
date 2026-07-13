using UrlShortner.Entities;

namespace UrlShortner.Services;

public interface IUrlShorteningService
{
    Task<ShortenedUrl> ShortenAsync(string longUrl, string baseUrl);
    Task<string?> GetLongUrlAsync(string code);
}