using System.Collections.Concurrent;
using UrlShortner.Entities;

namespace UrlShortner.Services;

public class InMemoryUrlStore : IUrlStore
{
    private readonly ConcurrentDictionary<string, ShortenedUrl> _store = new();

    public Task<bool> ExistsAsync(string code) =>
        Task.FromResult(_store.ContainsKey(code));

    public Task SaveAsync(ShortenedUrl url)
    {
        _store[url.Code] = url;
        return Task.CompletedTask;
    }

    public Task<ShortenedUrl?> GetByCodeAsync(string code) =>
        Task.FromResult(_store.GetValueOrDefault(code));
}