using System.Text;

namespace MysteryMud.Core.Extensions;

public static class StringBuilderExtensions
{
    public static void PluralizeLastWord(this StringBuilder sb)
    {
        if (sb == null || sb.Length == 0)
            return;

        int end = sb.Length - 1;

        // Skip trailing non-letters (spaces, punctuation, etc.)
        while (end >= 0 && !char.IsLetter(sb[end]))
            end--;

        if (end < 0)
            return;

        int start = end;

        // Find start of the word
        while (start >= 0 && char.IsLetter(sb[start]))
            start--;

        start++; // move to first letter

        int length = end - start + 1;

        string word = sb.ToString(start, length);
        string plural = Pluralizer.Pluralize(word);

        // Replace in-place
        sb.Remove(start, length);
        sb.Insert(start, plural);
    }
}
