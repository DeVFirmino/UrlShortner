using UrlShortner.Contracts;
using UrlShortner.Entities;
using UrlShortner.Errors;
using UrlShortner.Infrastructure.ShortCodes;
using UrlShortner.Infrastructure.Storage;

namespace UrlShortner.UseCases.ShortenUrl;

public sealed class ShortenUrlUseCase : IShortenUrlUseCase
{
    // A draw only fails when the code is already taken. Against 62^7 codes a run
    // of five losses is a broken generator or a broken store, so stop rather
    // than retry for ever and hold the request open.
    private const int MaxCodeAttempts = 5;

    private readonly IShortCodeGenerator _generator;
    private readonly IUrlStore _store;

    public ShortenUrlUseCase(IShortCodeGenerator generator, IUrlStore store)
    {
        _generator = generator;
        _store = store;
    }

    public async Task<ShortenUrlResponse> Execute(
        ShortenUrlRequest request,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        Validate(request);

        for (int attempt = 1; attempt <= MaxCodeAttempts; attempt++)
        {
            string code = _generator.Generate();

            ShortenedUrl candidate = new()
            {
                Id = Guid.NewGuid(),
                LongUrl = request.Url,
                Code = code,
                ShortUrl = $"{baseUrl}/{code}",
                CreatedOnUtc = DateTime.UtcNow,
            };

            // Draw and claim, never draw, ask and then claim: the store decides
            // the winner, so a code is only ever handed out once.
            if (await _store.TryInsertAsync(candidate, cancellationToken))
            {
                return new ShortenUrlResponse
                {
                    Code = candidate.Code,
                    ShortUrl = candidate.ShortUrl,
                };
            }
        }

        throw new ShortCodeCollisionException(MaxCodeAttempts);
    }

    private static void Validate(ShortenUrlRequest request)
    {
        if (Uri.TryCreate(request.Url, UriKind.Absolute, out Uri? url) is false ||
            (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidUrlException(request.Url);
        }
    }
}
