using System.Text.Json;
using StackExchange.Redis;
using UrlShortner.Entities;
using IDatabase = StackExchange.Redis.IDatabase;

namespace UrlShortner.Infrastructure.Storage;

public sealed class CachedUrlStore : IUrlStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly IUrlStore _inner;
    private readonly IDatabase _cache;

    public CachedUrlStore(IUrlStore inner, IConnectionMultiplexer redis)
    {
        _inner = inner;
        _cache = redis.GetDatabase();
    }

    public async Task<bool> TryInsertAsync(ShortenedUrl url, CancellationToken cancellationToken)
    {
        // Only cache a code this call actually claimed. Caching a rejected insert
        // would serve someone else's destination from the cache.
        if (await _inner.TryInsertAsync(url, cancellationToken) is false)
        {
            return false;
        }

        await _cache.StringSetAsync(KeyFor(url.Code), JsonSerializer.Serialize(url), Ttl);

        return true;
    }

    public async Task<ShortenedUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        string key = KeyFor(code);

        RedisValue cached = await _cache.StringGetAsync(key);

        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<ShortenedUrl>(cached!);
        }

        ShortenedUrl? stored = await _inner.GetByCodeAsync(code, cancellationToken);

        if (stored is null)
        {
            return null;
        }

        await _cache.StringSetAsync(key, JsonSerializer.Serialize(stored), Ttl);

        return stored;
    }

    private static string KeyFor(string code) => $"url:{code}";
}
