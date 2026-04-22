using System.Runtime.CompilerServices;

namespace MysteryMud.Core.Utilities;

public class WordTrie<T>
{
    private readonly WordTrieNode _root = new();

    public void Insert(string input, T value)
    {
        var tokens = Tokenize(input);
        var node = _root;

        foreach (var token in tokens)
        {
            char key = ToLowerAscii(token[0]);

            if (!node.Groups.TryGetValue(key, out var group))
            {
                group = [];
            }

            WordTrieNode? next = null;
            int index = -1;

            // search inside group only
            for (int i = 0; i < group.Length; i++)
            {
                if (group[i].Word == token)
                {
                    next = group[i].Node;
                    index = i;
                    break;
                }
            }

            if (next == null)
            {
                next = new WordTrieNode();

                Array.Resize(ref group, group.Length + 1);
                group[^1] = (token, next);

                node.Groups[key] = group;
            }

            node = next;
            node.FirstDescendantValue ??= value;
        }

        node.Value = value;
    }

    // TODO: return a result: found, ambiguous, not found
    public StartsWithResult StartsWith(ReadOnlySpan<char> input, out T? value)
    {
        var node = _root;
        int index = 0;

        while (TryReadNextToken(input, ref index, out var token))
        {
            var match = FindMatch(node, token, out bool ambiguous);

            if (ambiguous || match == null)
            {
                value = default;
                return StartsWithResult.Ambiguous;
            }

            node = match;
        }

        value = node.FirstDescendantValue ?? node.Value;
        return value != null
            ? StartsWithResult.Found
            : StartsWithResult.NotFound;
    }

    // match token against children using prefix
    private static WordTrieNode? FindMatch(WordTrieNode node, ReadOnlySpan<char> token, out bool ambiguous)
    {
        ambiguous = false;

        if (token.IsEmpty)
            return null;

        char key = ToLowerAscii(token[0]);

        if (!node.Groups.TryGetValue(key, out var group))
            return null;

        WordTrieNode? found = null;

        foreach (var (word, child) in group)
        {
            if (!StartsWithIgnoreCase(word, token))
                continue;

            if (found != null)
            {
                ambiguous = true;
                return null;
            }

            found = child;
        }

        return found;
    }

    // helpers
    private static bool TryReadNextToken(ReadOnlySpan<char> span, ref int index, out ReadOnlySpan<char> token)
    {
        int length = span.Length;

        // 1. Skip whitespace
        while (index < length && span[index] == ' ')
            index++;

        // End of input
        if (index >= length)
        {
            token = default;
            return false;
        }

        // 2. Start of token
        int start = index;

        // 3. Scan until next space
        while (index < length && span[index] != ' ')
            index++;

        token = span.Slice(start, index - start);
        return true;
    }

    private static bool StartsWithIgnoreCase(string word, ReadOnlySpan<char> token)
    {
        if (token.Length > word.Length)
            return false;

        for (int i = 0; i < token.Length; i++)
        {
            char a = word[i];
            char b = token[i];

            if (ToLowerAscii(word[i]) != ToLowerAscii(token[i]))
                return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToLowerAscii(char c)
        => (char)(c | 0x20);

    private static string[] Tokenize(string input)
        => input
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public sealed class WordTrieNode
    {
        // key = lowercase first character
        public Dictionary<char, (string Word, WordTrieNode Node)[]> Groups = [];

        public T? Value;
        public T? FirstDescendantValue;

        public bool HasValue => Value != null;
    }
}


//public class WordTrie<T>
//{
//    private readonly WordTrieNode _root = new();

//    public void Insert(string input, T value)
//    {
//        var tokens = Tokenize(input); // OK to allocate at build time
//        var node = _root;

//        foreach (var token in tokens)
//        {
//            WordTrieNode? next = null;
//            int index = -1;

//            // find existing child
//            for (int i = 0; i < node.Children.Length; i++)
//            {
//                if (node.Children[i].Word == token)
//                {
//                    next = node.Children[i].Node;
//                    index = i;
//                    break;
//                }
//            }

//            if (next == null)
//            {
//                next = new WordTrieNode();

//                Array.Resize(ref node.Children, node.Children.Length + 1);
//                node.Children[^1] = (token, next);
//            }

//            node = next;

//            node.FirstDescendantValue ??= value;
//        }

//        node.Value = value;
//    }

//    public bool StartsWith(ReadOnlySpan<char> input, out T? value)
//    {
//        var node = _root;
//        var span = input;

//        while (true)
//        {
//            span = TrimStart(span);

//            if (span.IsEmpty)
//                break;

//            ReadToken(span, out var token, out var rest);

//            var match = FindMatch(node, token, out bool ambiguous);

//            if (ambiguous || match == null)
//            {
//                value = default;
//                return false;
//            }

//            node = match;
//            span = rest;
//        }

//        value = node.FirstDescendantValue ?? node.Value;
//        return value != null;
//    }

//    // match token against children using prefix
//    private static WordTrieNode? FindMatch(
//        WordTrieNode node,
//        ReadOnlySpan<char> token,
//        out bool ambiguous)
//    {
//        WordTrieNode? found = null;
//        ambiguous = false;

//        foreach (var (word, child) in node.Children)
//        {
//            if (!StartsWithIgnoreCase(word, token))
//                continue;

//            if (found != null)
//            {
//                ambiguous = true;
//                return null;
//            }

//            found = child;
//        }

//        return found;
//    }

//    // helpers

//    private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
//    {
//        int i = 0;
//        while (i < span.Length && span[i] == ' ')
//            i++;
//        return span[i..];
//    }

//    private static void ReadToken(ReadOnlySpan<char> span, out ReadOnlySpan<char> token, out ReadOnlySpan<char> rest)
//    {
//        int i = 0;
//        while (i < span.Length && span[i] != ' ')
//            i++;

//        token = span[..i];
//        rest = span[i..];
//    }

//    private static bool StartsWithIgnoreCase(string word, ReadOnlySpan<char> token)
//    {
//        if (token.Length > word.Length)
//            return false;

//        for (int i = 0; i < token.Length; i++)
//        {
//            char a = word[i];
//            char b = token[i];

//            // fast ASCII lowercase
//            if (a >= 'A' && a <= 'Z') a = (char)(a + 32);
//            if (b >= 'A' && b <= 'Z') b = (char)(b + 32);

//            if (a != b)
//                return false;
//        }

//        return true;
//    }

//    private static string[] Tokenize(string input)
//        => input
//            .Trim()
//            .ToLowerInvariant()
//            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

//    public sealed class WordTrieNode
//    {
//        // Small set → array is faster than Dictionary
//        public (string Word, WordTrieNode Node)[] Children = [];

//        public T? Value;
//        public T? FirstDescendantValue;

//        public bool HasValue => Value != null;
//    }
//}
