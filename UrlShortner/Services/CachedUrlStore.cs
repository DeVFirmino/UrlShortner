using System.Text.Json;
using StackExchange.Redis;
using UrlShortner.Entities;
using IDatabase = StackExchange.Redis.IDatabase;   

namespace UrlShortner.Services;

public class CachedUrlStore : IUrlStore
{
    private readonly  IUrlStore _inner;
    private readonly IDatabase _cache; 
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    
    public CachedUrlStore(IUrlStore inner, IConnectionMultiplexer redis)
    {
        _inner = inner;
        _cache = redis.GetDatabase();
    }
    public Task<bool> ExistsAsync(string code) => _inner.ExistsAsync(code);

    public async Task SaveAsync(ShortenedUrl url)
    {
        await _inner.SaveAsync(url);
        await _cache.StringSetAsync($"url:{url.Code}", JsonSerializer.Serialize(url), Ttl);
    }

    public async Task<ShortenedUrl?> GetByCodeAsync(string code)
    {
        var key = $"url:{code}";
        
        var cached = await _cache.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<ShortenedUrl>(cached!);

        var entity = await _inner.GetByCodeAsync(code);
        if (entity is null)
            return null;

        await _cache.StringSetAsync(key, JsonSerializer.Serialize(entity), Ttl);
        return entity;
    }
}