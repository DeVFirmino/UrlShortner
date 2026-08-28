namespace UrlShortner.Infrastructure.ShortCodes;

public sealed class ShortCodeGenerator : IShortCodeGenerator
{
    // Base62: 10 digits + 26 lowercase + 26 uppercase, every one of them safe in
    // a URL without escaping.
    private const string Alphabet =
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // 7 characters give 62^7, roughly 3.5 trillion codes.
    public const int CodeLength = 7;

    public string Generate()
    {
        char[] buffer = new char[CodeLength];

        for (int index = 0; index < CodeLength; index++)
        {
            buffer[index] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
