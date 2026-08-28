namespace UrlShortner.Errors;

public static class ErrorMessages
{
    public const string InvalidUrl = "The url must be an absolute http or https address.";

    public const string ShortCodeNotFound = "Short code not found.";

    public const string ShortCodeUnavailable =
        "Could not issue a short code right now. Please try again.";

    public const string UnexpectedError = "Unexpected error.";
}
