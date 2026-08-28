using System.Net;

namespace UrlShortner.Errors;

public sealed class ShortCodeNotFoundException : UrlShortnerException
{
    public ShortCodeNotFoundException(string code)
        : base(ErrorMessages.ShortCodeNotFound) => Code = code;

    public string Code { get; }

    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;

    public override IReadOnlyList<string> Errors => [Message];
}
