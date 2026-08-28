using System.Net;

namespace UrlShortner.Errors;

/// <summary>
/// Thrown when several draws in a row all landed on codes that were already
/// taken. Against a 3.5-trillion code space this means the generator or the
/// store is misbehaving, not that the caller did anything wrong, so the caller
/// is told to retry rather than that the request was bad.
/// </summary>
public sealed class ShortCodeCollisionException : UrlShortnerException
{
    public ShortCodeCollisionException(int attempts)
        : base(ErrorMessages.ShortCodeUnavailable) => Attempts = attempts;

    public int Attempts { get; }

    public override HttpStatusCode StatusCode => HttpStatusCode.ServiceUnavailable;

    public override IReadOnlyList<string> Errors => [Message];
}
