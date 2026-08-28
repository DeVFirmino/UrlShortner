using Cassandra;
using UrlShortner.Entities;
using ISession = Cassandra.ISession;

namespace UrlShortner.Infrastructure.Storage;

public sealed class CassandraUrlStore : IUrlStore
{
    private readonly ISession _session;

    public CassandraUrlStore(ISession session)
    {
        _session = session;
    }

    public async Task<bool> TryInsertAsync(ShortenedUrl url, CancellationToken cancellationToken)
    {
        // The Cassandra driver's async API takes no CancellationToken, so the
        // token is honoured at the boundary rather than passed on.
        cancellationToken.ThrowIfCancellationRequested();

        // IF NOT EXISTS turns the upsert into a lightweight transaction: Cassandra
        // runs a Paxos round for the partition, so of several concurrent inserts
        // on the same code exactly one is applied and the rest are rejected.
        PreparedStatement statement = await _session.PrepareAsync(
            """
            INSERT INTO shortened_urls (code, id, long_url, short_url, created_on_utc)
            VALUES (?, ?, ?, ?, ?)
            IF NOT EXISTS
            """);

        RowSet rows = await _session.ExecuteAsync(statement.Bind(
            url.Code, url.Id, url.LongUrl, url.ShortUrl, url.CreatedOnUtc));

        // A conditional insert always answers with an [applied] column. If the row
        // is missing something is wrong, so report "not applied" and let the
        // caller draw again rather than assume the write landed.
        Row? row = rows.FirstOrDefault();

        return row is not null && row.GetValue<bool>("[applied]");
    }

    public async Task<ShortenedUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PreparedStatement statement = await _session.PrepareAsync(
            """
            SELECT code, id, long_url, short_url, created_on_utc
            FROM shortened_urls
            WHERE code = ?
            """);

        RowSet rows = await _session.ExecuteAsync(statement.Bind(code));
        Row? row = rows.FirstOrDefault();

        if (row is null)
        {
            return null;
        }

        return new ShortenedUrl
        {
            Code = row.GetValue<string>("code"),
            Id = row.GetValue<Guid>("id"),
            LongUrl = row.GetValue<string>("long_url"),
            ShortUrl = row.GetValue<string>("short_url"),
            CreatedOnUtc = row.GetValue<DateTime>("created_on_utc"),
        };
    }
}
