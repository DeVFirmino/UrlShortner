using UrlShortner.Entities;

namespace UrlShortner.Services;

public interface IUrlStore
{
    Task<bool> ExistsAsync(string code);
    Task SaveAsync(ShortenedUrl url);
    Task<ShortenedUrl?> GetByCodeAsync(string code);
}