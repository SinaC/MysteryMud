namespace MysteryMud.Core.Extensions;

public static class ReadOnlySpanCharExtensions
{
    public static int ComputeUniqueId(this ReadOnlySpan<char> name)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in name)
                hash = hash * 31 + char.ToLowerInvariant(c);
            return hash;
        }
    }
}
