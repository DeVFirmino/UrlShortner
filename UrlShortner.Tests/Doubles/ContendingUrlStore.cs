using UrlShortner.Entities;
using UrlShortner.Infrastructure.Storage;

namespace UrlShortner.Tests.Doubles;

/// <summary>
/// Makes concurrent callers contend for a code at the same instant instead of
/// leaving the interleaving to the operating system scheduler.
/// <para>
/// The hold sits immediately before the store decides the winner, so the first
/// <c>contendingCallers</c> callers all reach that decision together. Holding
/// them any earlier — at the draw, for instance — proves nothing: the first
/// caller released finishes claiming its code before the next one wakes, the
/// two never overlap, and a store that races would still look correct.
/// </para>
/// </summary>
public sealed class ContendingUrlStore : IUrlStore, IDisposable
{
    private static readonly TimeSpan ContentionTimeout = TimeSpan.FromSeconds(10);

    private readonly IUrlStore _inner;
    private readonly CountdownEvent _callersAtTheDecision;
    private readonly int _contendingCallers;

    private int _arrivals;

    public ContendingUrlStore(IUrlStore inner, int contendingCallers)
    {
        _inner = inner;
        _contendingCallers = contendingCallers;
        _callersAtTheDecision = new CountdownEvent(contendingCallers);
    }

    public Task<bool> TryInsertAsync(ShortenedUrl url, CancellationToken cancellationToken)
    {
        HoldUntilEveryCallerIsAtTheDecision();

        return _inner.TryInsertAsync(url, cancellationToken);
    }

    public Task<ShortenedUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _inner.GetByCodeAsync(code, cancellationToken);

    public void Dispose() => _callersAtTheDecision.Dispose();

    private void HoldUntilEveryCallerIsAtTheDecision()
    {
        // Only the opening round is held. A caller that lost and came back to
        // draw again must not wait for companions that no longer exist.
        if (Interlocked.Increment(ref _arrivals) > _contendingCallers)
        {
            return;
        }

        _callersAtTheDecision.Signal();

        if (_callersAtTheDecision.Wait(ContentionTimeout) is false)
        {
            throw new TimeoutException(
                "The contending callers never met at the claim.");
        }
    }
}
