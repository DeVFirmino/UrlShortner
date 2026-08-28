namespace UrlShortner.UseCases.ResolveShortCode;

public interface IResolveShortCodeUseCase
{
    Task<string> Execute(string code, CancellationToken cancellationToken);
}
