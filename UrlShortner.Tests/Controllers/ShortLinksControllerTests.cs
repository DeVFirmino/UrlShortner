using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UrlShortner.Contracts;
using UrlShortner.Controllers;
using UrlShortner.Errors;
using UrlShortner.Filters;
using UrlShortner.Tests.Doubles;
using UrlShortner.UseCases.ResolveShortCode;
using UrlShortner.UseCases.ShortenUrl;

namespace UrlShortner.Tests.Controllers;

/// <summary>
/// Drives the controllers over real HTTP with the use cases stubbed, so the
/// routes, the binding, the status codes and the exception filter are all
/// checked without a Cassandra or a Redis to talk to.
/// </summary>
public class ShortLinksControllerTests
{
    private const string Code = "aaaaaaa";
    private const string Destination = "https://example.com/page";

    [Fact]
    public async Task ShouldReturnCreatedWithTheShortLinkWhenTheUrlIsShortened()
    {
        using TestServer server = BuildServer(
            shorten: (request, baseUrl) => new ShortenUrlResponse
            {
                Code = Code,
                ShortUrl = $"{baseUrl}/{Code}",
            });
        using HttpClient client = server.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/shorten",
            new ShortenUrlRequest { Url = Destination });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith($"/{Code}");

        ShortenUrlResponse? body = await response.Content
            .ReadFromJsonAsync<ShortenUrlResponse>();

        body!.Code.Should().Be(Code);
        body.ShortUrl.Should().EndWith($"/{Code}");
    }

    [Fact]
    public async Task ShouldReturnBadRequestWithTheErrorContractWhenTheUrlIsInvalid()
    {
        using TestServer server = BuildServer(
            shorten: (request, baseUrl) => throw new InvalidUrlException(request.Url));
        using HttpClient client = server.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/shorten",
            new ShortenUrlRequest { Url = "not a url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        ErrorResponse? body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        body!.Errors.Should().ContainSingle().Which.Should().Be(ErrorMessages.InvalidUrl);
    }

    [Fact]
    public async Task ShouldReturnServiceUnavailableWhenNoCodeCouldBeIssued()
    {
        using TestServer server = BuildServer(
            shorten: (request, baseUrl) => throw new ShortCodeCollisionException(5));
        using HttpClient client = server.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/shorten",
            new ShortenUrlRequest { Url = Destination });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ShouldRedirectToTheOriginalUrlWhenTheCodeIsKnown()
    {
        using TestServer server = BuildServer(resolve: code => Destination);
        using HttpClient client = server.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/{Code}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(Destination);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheCodeIsUnknown()
    {
        using TestServer server = BuildServer(
            resolve: code => throw new ShortCodeNotFoundException(code));
        using HttpClient client = server.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/{Code}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        ErrorResponse? body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        body!.Errors.Should().ContainSingle().Which.Should().Be(ErrorMessages.ShortCodeNotFound);
    }

    [Theory]
    [InlineData("/favicon.ico")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/toolong8")]
    [InlineData("/short")]
    public async Task ShouldNotTreatAPathThatCannotBeACodeAsAShortLink(string path)
    {
        using TestServer server = BuildServer(
            resolve: code => throw new InvalidOperationException(
                "a path that cannot be a seven character code must never reach the use case"));
        using HttpClient client = server.CreateClient();

        HttpResponseMessage response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNameTheAnsweringReplicaWhenAskedWhoAmI()
    {
        using TestServer server = BuildServer();
        using HttpClient client = server.CreateClient();

        ServerIdentityResponse? body = await client
            .GetFromJsonAsync<ServerIdentityResponse>("/whoami");

        body!.Server.Should().Be(Environment.MachineName);
    }

    private static TestServer BuildServer(
        Func<ShortenUrlRequest, string, ShortenUrlResponse>? shorten = null,
        Func<string, string>? resolve = null)
    {
        IWebHostBuilder builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.None));

                services.AddSingleton<IShortenUrlUseCase>(new StubShortenUrlUseCase(
                    shorten ?? ((request, baseUrl) => throw new InvalidOperationException(
                        "shortening was not expected in this test"))));

                services.AddSingleton<IResolveShortCodeUseCase>(new StubResolveShortCodeUseCase(
                    resolve ?? (code => throw new InvalidOperationException(
                        "resolving was not expected in this test"))));

                services
                    .AddControllers(options => options.Filters.Add<ExceptionFilter>())
                    .AddApplicationPart(typeof(ShortLinksController).Assembly);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });

        return new TestServer(builder);
    }
}
