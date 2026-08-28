using System.Collections.Concurrent;
using UrlShortner.Entities;

namespace UrlShortner.Infrastructure.Storage;

/// <summary>
/// The reference <see cref="IUrlStore"/>: no cache, no cluster, just the
/// claim-or-lose contract. Nothing registers it, because the running app
/// always talks to Cassandra; it exists so the contract can be exercised
/// without one, and so the tests measure the rule rather than the driver.
/// </summary>
public sealed class InMemoryUrlStore : IUrlStore
{
    private readonly ConcurrentDictionary<string, ShortenedUrl> _store = new();

    // TryAdd is the in-memory counterpart of Cassandra's INSERT ... IF NOT
    // EXISTS: exactly one of several concurrent callers is told it added the key.
    public Task<bool> TryInsertAsync(ShortenedUrl url, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_store.TryAdd(url.Code, url));
    }

    public Task<ShortenedUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_store.GetValueOrDefault(code));
    }
}
