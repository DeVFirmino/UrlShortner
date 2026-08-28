using UrlShortner.Contracts;
using UrlShortner.UseCases.ShortenUrl;

namespace UrlShortner.Tests.Doubles;

/// <summary>
/// Stands in for the shortening use case so a controller test exercises
/// routing, binding and the error contract rather than the store.
/// </summary>
public sealed class StubShortenUrlUseCase : IShortenUrlUseCase
{
    private readonly Func<ShortenUrlRequest, string, ShortenUrlResponse> _execute;

    public StubShortenUrlUseCase(Func<ShortenUrlRequest, string, ShortenUrlResponse> execute)
    {
        _execute = execute;
    }

    public Task<ShortenUrlResponse> Execute(
        ShortenUrlRequest request,
        string baseUrl,
        CancellationToken cancellationToken) =>
        Task.FromResult(_execute(request, baseUrl));
}
