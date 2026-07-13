namespace UrlShortner.Services;

public class ShortCodeGenerator
{
    
    //base 62 - 10 digits + 26 minchar + 26max char = 62 symbols
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    // 7 char => 62^7 ≈ 3,5 trilhões de códigos possíveis.
    public const int CodeLength = 7;
    
    // SORT CodeLength char do alphabet.
     public string Generate()
    {
        var buffer = new char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }
        return new string(buffer);
    }
}