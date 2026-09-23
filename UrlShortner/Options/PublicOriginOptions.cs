namespace UrlShortner.Options;

public sealed class PublicOriginOptions
{
    public const string SectionName = "PublicOrigin";

    public string BaseUrl { get; set; } = string.Empty;

    public static bool IsValid(string? baseUrl)
    {
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri)
            && uri.Scheme is ("http" or "https")
            && uri.UserInfo.Length == 0
            && uri.AbsolutePath == "/"
            && uri.Query.Length == 0
            && uri.Fragment.Length == 0;
    }
}
