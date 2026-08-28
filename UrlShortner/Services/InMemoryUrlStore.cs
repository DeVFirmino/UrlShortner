using System.Collections.Concurrent;
using UrlShortner.Entities;

namespace UrlShortner.Services;

public class InMemoryUrlStore : IUrlStore
{
    private readonly ConcurrentDictionary<string, ShortenedUrl> _store = new();

    // TryAdd is the in-memory counterpart of Cassandra's INSERT ... IF NOT
    // EXISTS: exactly one of several concurrent callers is told it added the key.
    public Task<bool> TryInsertAsync(ShortenedUrl url) =>
        Task.FromResult(_store.TryAdd(url.Code, url));

    public Task<ShortenedUrl?> GetByCodeAsync(string code) =>
        Task.FromResult(_store.GetValueOrDefault(code));
}
