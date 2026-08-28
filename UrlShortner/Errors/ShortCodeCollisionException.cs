namespace UrlShortner.Errors;

/// <summary>
/// Thrown when several draws in a row all landed on codes that were already
/// taken. Against a 3.5-trillion code space this means the generator or the
/// store is misbehaving, not that the caller did anything wrong.
/// </summary>
public class ShortCodeCollisionException : Exception
{
    public ShortCodeCollisionException(int attempts)
        : base($"Could not find a free short code in {attempts} attempts.")
    {
        Attempts = attempts;
    }

    public int Attempts { get; }
}
