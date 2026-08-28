using FluentAssertions;
using UrlShortner.Contracts;
using UrlShortner.Errors;
using UrlShortner.Infrastructure.Storage;
using UrlShortner.Tests.Doubles;
using UrlShortner.UseCases.ResolveShortCode;
using UrlShortner.UseCases.ShortenUrl;

namespace UrlShortner.Tests.UseCases.ShortenUrl;

public sealed class ShortenUrlUseCaseTests
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
        ShortenUrlUseCase useCase = new(generator, store);
        ResolveShortCodeUseCase resolve = new(store);

        Task<ShortenUrlResponse> first = Task.Run(
            () => useCase.Execute(Requesting(FirstDestination), BaseUrl, default));
        Task<ShortenUrlResponse> second = Task.Run(
            () => useCase.Execute(Requesting(SecondDestination), BaseUrl, default));

        ShortenUrlResponse[] shortened = await Task.WhenAll(first, second);

        shortened.Select(response => response.Code).Should().OnlyHaveUniqueItems(
            "a code that one caller already claimed must never be handed to another");

        shortened.Should().ContainSingle(response => response.Code == CollidingCode,
            "exactly one of the two callers may keep the code they both drew");

        (await resolve.Execute(shortened[0].Code, default))
            .Should().Be(FirstDestination);
        (await resolve.Execute(shortened[1].Code, default))
            .Should().Be(SecondDestination);
    }

    [Fact]
    public async Task ShouldDrawAnotherCodeWhenTheDrawnCodeIsAlreadyTaken()
    {
        InMemoryUrlStore store = new();
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: 1);
        ShortenUrlUseCase useCase = new(generator, store);
        ResolveShortCodeUseCase resolve = new(store);

        ShortenUrlResponse taken = await useCase.Execute(
            Requesting(FirstDestination), BaseUrl, default);
        ShortenUrlResponse redrawn = await useCase.Execute(
            Requesting(SecondDestination), BaseUrl, default);

        taken.Code.Should().Be(CollidingCode);
        redrawn.Code.Should().NotBe(CollidingCode);

        (await resolve.Execute(taken.Code, default)).Should().Be(FirstDestination);
        (await resolve.Execute(redrawn.Code, default)).Should().Be(SecondDestination);
    }

    [Fact]
    public async Task ShouldBuildTheShortUrlFromTheBaseUrlWhenTheCodeIsClaimed()
    {
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: 1);
        ShortenUrlUseCase useCase = new(generator, new InMemoryUrlStore());

        ShortenUrlResponse response = await useCase.Execute(
            Requesting(FirstDestination), BaseUrl, default);

        response.ShortUrl.Should().Be($"{BaseUrl}/{CollidingCode}");
    }

    [Fact]
    public async Task ShouldFailWhenEveryAttemptDrawsATakenCode()
    {
        InMemoryUrlStore store = new();
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: int.MaxValue);
        ShortenUrlUseCase useCase = new(generator, store);
        ResolveShortCodeUseCase resolve = new(store);

        await useCase.Execute(Requesting(FirstDestination), BaseUrl, default);

        Func<Task> shortening = () =>
            useCase.Execute(Requesting(SecondDestination), BaseUrl, default);

        await shortening.Should().ThrowAsync<ShortCodeCollisionException>(
            "a caller must be told the code could not be issued rather than wait for ever");

        (await resolve.Execute(CollidingCode, default)).Should().Be(
            FirstDestination,
            "a failed attempt must not disturb the link that already holds the code");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("/only/a/path")]
    [InlineData("ftp://example.com/file")]
    public async Task ShouldRejectTheRequestWhenTheUrlIsNotAnAbsoluteWebAddress(string url)
    {
        CollidingShortCodeGenerator generator = new(CollidingCode, collidingDraws: 1);
        ShortenUrlUseCase useCase = new(generator, new InMemoryUrlStore());

        Func<Task> shortening = () => useCase.Execute(Requesting(url), BaseUrl, default);

        await shortening.Should().ThrowAsync<InvalidUrlException>();
    }

    private static ShortenUrlRequest Requesting(string url) => new() { Url = url };
}
