using UrlShortner.Entities;

namespace UrlShortner.Services;

public class UrlShorteningService : IUrlShorteningService
{
    public readonly ShortCodeGenerator _generator;
    public readonly IUrlStore _store;
    
    public UrlShorteningService(ShortCodeGenerator generator, IUrlStore store)
    {
        _generator = generator;
        _store = store;
    }

    public async Task<ShortenedUrl> ShortenAsync(string longUrl, string baseUrl)
    {
        string code;
        do
        {
            code = _generator.Generate();
        } while (await _store.ExistsAsync(code));

        var entity = new ShortenedUrl
        {
            Id = Guid.NewGuid(),
            LongUrl = longUrl,
            Code = code,
            ShortUrl = $"{baseUrl}/{code}",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _store.SaveAsync(entity);
        return entity;
    }

    public async Task<string?> GetLongUrlAsync(string code)
        {
            var entity = await _store.GetByCodeAsync(code);
            return entity?.LongUrl;
        }
}
