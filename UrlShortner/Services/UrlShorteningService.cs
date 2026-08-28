using UrlShortner.Entities;
using UrlShortner.Errors;

namespace UrlShortner.Services;

public class UrlShorteningService : IUrlShorteningService
{
    // A draw only fails when the code is already taken. Against 62^7 codes a run
    // of five losses is a broken generator or a broken store, so stop rather
    // than retry for ever and hold the request open.
    private const int MaxCodeAttempts = 5;

    public readonly IShortCodeGenerator _generator;
    public readonly IUrlStore _store;
    
    public UrlShorteningService(IShortCodeGenerator generator, IUrlStore store)
    {
        _generator = generator;
        _store = store;
    }

    public async Task<ShortenedUrl> ShortenAsync(string longUrl, string baseUrl)
    {
        for (int attempt = 1; attempt <= MaxCodeAttempts; attempt++)
        {
            var code = _generator.Generate();

            var entity = new ShortenedUrl
            {
                Id = Guid.NewGuid(),
                LongUrl = longUrl,
                Code = code,
                ShortUrl = $"{baseUrl}/{code}",
                CreatedOnUtc = DateTime.UtcNow
            };

            // Draw and claim, never draw, ask and then claim: the store decides
            // the winner, so a code is only ever handed out once.
            if (await _store.TryInsertAsync(entity))
            {
                return entity;
            }
        }

        throw new ShortCodeCollisionException(MaxCodeAttempts);
    }

    public async Task<string?> GetLongUrlAsync(string code)
        {
            var entity = await _store.GetByCodeAsync(code);
            return entity?.LongUrl;
        }
}
