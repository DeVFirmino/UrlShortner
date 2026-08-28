using UrlShortner.Contracts;

namespace UrlShortner.UseCases.ShortenUrl;

public interface IShortenUrlUseCase
{
    Task<ShortenUrlResponse> Execute(
        ShortenUrlRequest request,
        string baseUrl,
        CancellationToken cancellationToken);
}
