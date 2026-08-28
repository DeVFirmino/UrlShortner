using FluentAssertions;
using UrlShortner.Entities;
using UrlShortner.Errors;
using UrlShortner.Infrastructure.Storage;
using UrlShortner.UseCases.ResolveShortCode;

namespace UrlShortner.Tests.UseCases.ResolveShortCode;

public class ResolveShortCodeUseCaseTests
{
    private const string KnownCode = "aaaaaaa";
    private const string Destination = "https://example.com/page";

    [Fact]
    public async Task ShouldReturnTheOriginalUrlWhenTheCodeExists()
    {
        InMemoryUrlStore store = new();
        await store.TryInsertAsync(Stored(KnownCode, Destination), default);
        ResolveShortCodeUseCase useCase = new(store);

        string resolved = await useCase.Execute(KnownCode, default);

        resolved.Should().Be(Destination);
    }

    [Fact]
    public async Task ShouldFailWhenTheCodeIsUnknown()
    {
        ResolveShortCodeUseCase useCase = new(new InMemoryUrlStore());

        Func<Task> resolving = () => useCase.Execute("zzzzzzz", default);

        await resolving.Should().ThrowAsync<ShortCodeNotFoundException>();
    }

    private static ShortenedUrl Stored(string code, string longUrl) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        LongUrl = longUrl,
        ShortUrl = $"https://sho.rt/{code}",
        CreatedOnUtc = DateTime.UtcNow,
    };
}
