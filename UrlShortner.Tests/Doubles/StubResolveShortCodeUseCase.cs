using UrlShortner.UseCases.ResolveShortCode;

namespace UrlShortner.Tests.Doubles;

/// <summary>
/// Stands in for the resolving use case so a controller test exercises routing
/// and the redirect contract rather than the store.
/// </summary>
public sealed class StubResolveShortCodeUseCase : IResolveShortCodeUseCase
{
    private readonly Func<string, string> _execute;

    public StubResolveShortCodeUseCase(Func<string, string> execute)
    {
        _execute = execute;
    }

    public Task<string> Execute(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_execute(code));
}
