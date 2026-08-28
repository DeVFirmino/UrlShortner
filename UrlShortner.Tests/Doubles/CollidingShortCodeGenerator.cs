using UrlShortner.Infrastructure.ShortCodes;

namespace UrlShortner.Tests.Doubles;

/// <summary>
/// Hands the very same code to the first <c>collidingDraws</c> callers, and a
/// distinct code to every draw after that, so a caller that loses the race for
/// the colliding code can still succeed on a re-draw.
/// </summary>
public sealed class CollidingShortCodeGenerator : IShortCodeGenerator
{
    private readonly string _collidingCode;
    private readonly int _collidingDraws;

    private int _draws;

    public CollidingShortCodeGenerator(string collidingCode, int collidingDraws)
    {
        _collidingCode = collidingCode;
        _collidingDraws = collidingDraws;
    }

    public string Generate()
    {
        int draw = Interlocked.Increment(ref _draws);

        return draw <= _collidingDraws
            ? _collidingCode
            : $"free{draw:D3}";
    }
}
