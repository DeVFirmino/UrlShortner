using FluentAssertions;
using UrlShortner.Entities;
using UrlShortner.Errors;
using UrlShortner.Services;
using UrlShortner.Tests.Doubles;

namespace UrlShortner.Tests;

public class UrlShorteningServiceTests
{
    private const string BaseUrl = "https://sho.rt";
    private const string CollidingCode = "aaaaaaa";
    private const string FirstDestination = "https://example.com/first";
    private const string SecondDestination = "https://example.com/second";
    private const int ContendingCallers = 2;

    [Fact]
    public async Task ShouldKeepEveryDestinationWhenConcurrentCallersDrawTheSameCode()
    {
        using ContendingUrlStore store = new(new InMemoryUrlStore(), ContendingCallers);
        CollidingShortCodeGenerator generator = new(CollidingCode, ContendingCallers);
        UrlShorteningService service = new(generator, store);

        Task<ShortenedUrl> first = Task.Run(
            () => service.ShortenAsync(FirstDestination, BaseUrl));
        Task<ShortenedUrl> second = Task.Run(
            () => service.ShortenAsync(SecondDestination, BaseUrl));

        ShortenedUrl[] shortened = await Task.WhenAll(first, second);

        shortened.Select(url => url.Code).Should().OnlyHaveUniqueItems(
            "a code that one caller already claimed must never be handed to another");

        shortened.Should().ContainSingle(url => url.Code == CollidingCode,
            "exactly one of the two callers may keep the code they both drew");

        foreach (ShortenedUrl url in shortened)
        {
            string? resolved = await service.GetLongUrlAsync(url.Code);

            resolved.Should().Be(
                url.LongUrl,
                "every short link must still resolve to the destination it was created for");
        }
    }

    [Fact]
    public async Task ShouldDrawAnotherCodeWhenTheDrawnCodeIsAlreadyTaken()
    {
        InMemoryUrlStore store = new();
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: 1);
        UrlShorteningService service = new(generator, store);

        ShortenedUrl taken = await service.ShortenAsync(FirstDestination, BaseUrl);
        ShortenedUrl redrawn = await service.ShortenAsync(SecondDestination, BaseUrl);

        taken.Code.Should().Be(CollidingCode);
        redrawn.Code.Should().NotBe(CollidingCode);

        (await service.GetLongUrlAsync(taken.Code)).Should().Be(FirstDestination);
        (await service.GetLongUrlAsync(redrawn.Code)).Should().Be(SecondDestination);
    }

    [Fact]
    public async Task ShouldResolveTheOriginalUrlWhenTheCodeExists()
    {
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: 1);
        UrlShorteningService service = new(generator, new InMemoryUrlStore());

        ShortenedUrl shortened = await service.ShortenAsync(FirstDestination, BaseUrl);

        shortened.ShortUrl.Should().Be($"{BaseUrl}/{CollidingCode}");
        (await service.GetLongUrlAsync(CollidingCode)).Should().Be(FirstDestination);
    }

    [Fact]
    public async Task ShouldReturnNullWhenTheCodeIsUnknown()
    {
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: 1);
        UrlShorteningService service = new(generator, new InMemoryUrlStore());

        string? resolved = await service.GetLongUrlAsync("zzzzzzz");

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task ShouldFailWhenEveryAttemptDrawsATakenCode()
    {
        InMemoryUrlStore store = new();
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: int.MaxValue);
        UrlShorteningService service = new(generator, store);

        await service.ShortenAsync(FirstDestination, BaseUrl);

        Func<Task> shortening = () => service.ShortenAsync(SecondDestination, BaseUrl);

        await shortening.Should().ThrowAsync<ShortCodeCollisionException>(
            "a caller must be told the code could not be issued rather than wait for ever");

        (await service.GetLongUrlAsync(CollidingCode)).Should().Be(
            FirstDestination,
            "a failed attempt must not disturb the link that already holds the code");
    }
}
