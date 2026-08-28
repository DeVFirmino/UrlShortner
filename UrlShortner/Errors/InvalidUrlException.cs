using System.Net;

namespace UrlShortner.Errors;

public sealed class InvalidUrlException : UrlShortnerException
{
    public InvalidUrlException(string url) : base(ErrorMessages.InvalidUrl) => Url = url;

    public string Url { get; }

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

    public override IReadOnlyList<string> Errors => [Message];
}
