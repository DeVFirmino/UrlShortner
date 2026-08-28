using UrlShortner.Entities;
using UrlShortner.Errors;
using UrlShortner.Infrastructure.Storage;

namespace UrlShortner.UseCases.ResolveShortCode;

public sealed class ResolveShortCodeUseCase : IResolveShortCodeUseCase
{
    private readonly IUrlStore _store;

    public ResolveShortCodeUseCase(IUrlStore store)
    {
        _store = store;
    }

    public async Task<string> Execute(string code, CancellationToken cancellationToken)
    {
        ShortenedUrl stored = await _store.GetByCodeAsync(code, cancellationToken)
            ?? throw new ShortCodeNotFoundException(code);

        return stored.LongUrl;
    }
}
