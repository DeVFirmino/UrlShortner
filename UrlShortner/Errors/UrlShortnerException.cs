using System.Net;

namespace UrlShortner.Errors;

public abstract class UrlShortnerException : Exception
{
    protected UrlShortnerException(string message) : base(message)
    {
    }

    public abstract HttpStatusCode StatusCode { get; }

    public abstract IReadOnlyList<string> Errors { get; }
}
