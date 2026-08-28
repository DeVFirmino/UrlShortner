using Microsoft.AspNetCore.Mvc;
using UrlShortner.Contracts;
using UrlShortner.UseCases.ResolveShortCode;
using UrlShortner.UseCases.ShortenUrl;

namespace UrlShortner.Controllers;

[ApiController]
public sealed class ShortLinksController : ControllerBase
{
    // The routes are literal and rooted rather than grouped under /api because
    // a short link has to be short: /aaaaaaa is the product, /api/short-links/
    // /aaaaaaa is not. nginx.conf, compose.yaml and the README all publish
    // these two paths.
    [HttpPost("/shorten")]
    [ProducesResponseType(typeof(ShortenUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ShortenUrl(
        [FromServices] IShortenUrlUseCase useCase,
        [FromBody] ShortenUrlRequest request,
        CancellationToken cancellationToken)
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}";

        ShortenUrlResponse response = await useCase.Execute(
            request,
            baseUrl,
            cancellationToken);

        return CreatedAtAction(
            nameof(ResolveShortCode),
            new { code = response.Code },
            response);
    }

    // The length constraint keeps paths that cannot be codes, such as
    // /favicon.ico or /openapi/v1.json, from being looked up as one. It cannot
    // separate a code from a same-length word: /swagger is itself seven base62
    // characters. That path is served by the Swagger middleware, which Program.cs
    // runs before MapControllers, so it never reaches this action.
    [HttpGet("/{code:length(7)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveShortCode(
        [FromServices] IResolveShortCodeUseCase useCase,
        [FromRoute] string code,
        CancellationToken cancellationToken)
    {
        string longUrl = await useCase.Execute(code, cancellationToken);

        // 302 rather than 301 so the redirect keeps coming back through the
        // server and clicks stay countable.
        return Redirect(longUrl);
    }
}
