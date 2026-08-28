using UrlShortner.Entities;

namespace UrlShortner.Services;

public interface IUrlStore
{
    /// <summary>
    /// Claims <paramref name="url"/>'s code and stores it, but only if no other
    /// caller holds that code already. Returns <c>true</c> when this call is the
    /// one that claimed it.
    /// </summary>
    /// <remarks>
    /// Claiming is one atomic operation on purpose. A separate "does this code
    /// exist?" question could only ever report the past: two callers can both be
    /// told a code is free and then both write it, and in Cassandra a plain
    /// INSERT is an upsert, so the second write silently replaces the first and
    /// a live short link starts pointing at someone else's destination.
    /// </remarks>
    Task<bool> TryInsertAsync(ShortenedUrl url);

    Task<ShortenedUrl?> GetByCodeAsync(string code);
}
