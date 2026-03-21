namespace MysteryMud.ConsoleApp3.Commands.v2;

public readonly struct Token
{
    public readonly int Start;
    public readonly int Length;

    public Token(int start, int length)
    {
        Start = start;
        Length = length;
    }

    // Get the span of this token from input
    public ReadOnlySpan<char> Slice(ReadOnlySpan<char> input) => input.Slice(Start, Length);

    public override string ToString() => $"Token({Start}, {Length})"; // for debugging
}
