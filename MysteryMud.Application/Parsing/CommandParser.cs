using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Targeting;

namespace MysteryMud.Application.Parsing;

public static class CommandParser
{
    public static void Parse(ReadOnlySpan<char> cmd, ReadOnlySpan<char> args, int argumentCount, bool lastIsText, out CommandContext ctx)
    {
        ctx = default;
        ctx.Command = cmd;

        var e = new ArgumentEnumerator(args);
        int consumed = 0;
        int parsed = 0;
        while (parsed < argumentCount && e.MoveNext())
        {
            var token = e.Current;

            // Skip link words
            if (IsLinkWord(token))
                continue;

            switch (parsed)
            {
                case 0: ctx.Primary = ParseTarget(e.Current); break;
                case 1: ctx.Secondary = ParseTarget(e.Current); break;
                case 2: ctx.Tertiary = ParseTarget(e.Current); break;
                case 3: ctx.Quaternary = ParseTarget(e.Current); break;
                case 4: ctx.Quinary = ParseTarget(e.Current); break;
            }
            parsed++;
            consumed = e.Consumed;
        }

        if (lastIsText && consumed < args.Length)
            ctx.Text = TrimMatchingQuotes(args[consumed..].TrimStart());

        ctx.TargetCount = parsed;
    }

    public static void SplitCommand(ReadOnlySpan<char> input, out int commandStart, out int commandLength, out int argsStart, out int argsLength)
    {
        input = input.Trim();
        int space = input.IndexOf(' ');

        if (space < 0)
        {
            commandStart = 0;
            commandLength = input.Length;
            argsStart = input.Length;
            argsLength = 0;
            return;
        }

        commandStart = 0;
        commandLength = space;
        argsStart = space+1;
        argsLength = input.Length - (space + 1);
    }

    private static TargetSpec ParseTarget(ReadOnlySpan<char> token)
    {
        TargetSpec spec = default;

        if (token.Equals("self".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            spec.Kind = TargetKind.Self;
            return spec;
        }

        if (token.Equals("all".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            spec.Kind = TargetKind.All;
            return spec;
        }

        int dot = token.IndexOf('.');
        if (dot > 0)
        {
            var left = token[..dot];
            var right = token[(dot + 1)..];

            if (left.Equals("all".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                spec.Kind = TargetKind.All;
                spec.Name = right;
                return spec;
            }

            if (int.TryParse(left, out int index))
            {
                spec.Kind = TargetKind.Indexed;
                spec.Index = index;
                spec.Name = right;
                return spec;
            }
        }

        spec.Kind = TargetKind.Single;
        spec.Name = token;
        return spec;
    }

    private ref struct ArgumentEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        public int Consumed { get; private set; }

        public ArgumentEnumerator(ReadOnlySpan<char> args)
        {
            _remaining = args.Trim();
            Current = default;
            Consumed = 0;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
                return false;

            _remaining = _remaining.TrimStart();

            if (_remaining.IsEmpty)
                return false;

            int tokenLength = 0;
            if (_remaining[0] == '"' || _remaining[0] == '\'')
            {
                // Quoted token
                char quote = _remaining[0];
                int end = _remaining[1..].IndexOf(quote);
                if (end < 0)
                {
                    // No closing quote — consume rest
                    tokenLength = _remaining.Length;
                    Current = _remaining[1..]; // exclude starting quote
                    _remaining = default;
                }
                else
                {
                    tokenLength = end + 2; // +2 to include both quotes
                    Current = _remaining.Slice(1, end); // exclude quotes
                    _remaining = _remaining[tokenLength..];
                }
            }
            else
            {
                // Unquoted token
                int space = _remaining.IndexOf(' ');
                if (space < 0)
                {
                    Current = _remaining;
                    tokenLength = _remaining.Length;
                    _remaining = default;
                }
                else
                {
                    Current = _remaining[..space];
                    tokenLength = space + 1;
                    _remaining = _remaining[tokenLength..];
                }
            }

            Consumed += tokenLength;
            return true;
        }
    }

    private static ReadOnlySpan<char> TrimMatchingQuotes(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return span;

        // Remove opening quote if present
        if (span[0] == '"' || span[0] == '\'')
            span = span[1..];

        // Remove closing quote if present
        if (!span.IsEmpty && (span[^1] == '"' || span[^1] == '\''))
            span = span[..^1];

        return span;
    }

    private static bool IsLinkWord(ReadOnlySpan<char> token)
    {
        return token.Equals("from", StringComparison.OrdinalIgnoreCase)
            || token.Equals("in", StringComparison.OrdinalIgnoreCase)
            || token.Equals("on", StringComparison.OrdinalIgnoreCase)
            || token.Equals("at", StringComparison.OrdinalIgnoreCase);
    }
}
//using MysteryMud.Core.Command;

//namespace MysteryMud.Infrastructure.Command;

//public class CommandParser : ICommandParser
//{
//    public void Parse(CommandParseMode parseMode, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args, out CommandContext ctx)
//    {
//        ctx = default;

//        ctx.Command = cmd;

//        switch(parseMode)
//        {
//            case CommandParseMode.None:
//                break;
//            case CommandParseMode.Target:
//                if (!args.IsEmpty)
//                    ctx.Primary = ParseTarget(args.Trim());
//                break;
//            case CommandParseMode.TargetPair:
//                {
//                    var e = new ArgumentEnumerator(args);

//                    if (e.MoveNext())
//                        ctx.Primary = ParseTarget(e.Current);

//                    if (e.MoveNext())
//                    {
//                        var next = e.Current;

//                        if (next.Equals("from".AsSpan(), StringComparison.OrdinalIgnoreCase) || next.Equals("in".AsSpan(), StringComparison.OrdinalIgnoreCase))
//                        {
//                            if (e.MoveNext())
//                                ctx.Secondary = ParseTarget(e.Current);
//                        }
//                        else
//                        {
//                            ctx.Secondary = ParseTarget(next);
//                        }
//                    }
//                    break;
//                }
//            case CommandParseMode.TargetAndText:
//                {
//                    var e = new ArgumentEnumerator(args);

//                    if (e.MoveNext())
//                    {
//                        ctx.Primary = ParseTarget(e.Current);

//                        int start = args.IndexOf(e.Current);
//                        if (start >= 0)
//                        {
//                            var rest = args[(start + e.Current.Length)..].TrimStart();
//                            ctx.Text = rest;
//                        }
//                    }

//                    break;
//                }
//            case CommandParseMode.FullText:
//                ctx.Text = args.Trim();
//                break;
//        }
//    }

//    public void SplitCommand(ReadOnlySpan<char> input, out ReadOnlySpan<char> command, out ReadOnlySpan<char> args)
//    {
//        input = input.Trim();

//        int space = input.IndexOf(' ');

//        if (space < 0)
//        {
//            command = input;
//            args = ReadOnlySpan<char>.Empty;
//            return;
//        }

//        command = input[..space];
//        args = input[(space + 1)..].TrimStart();
//    }

//    private static TargetSpec ParseTarget(ReadOnlySpan<char> token)
//    {
//        TargetSpec spec = default;

//        if (token.Equals("self".AsSpan(), StringComparison.OrdinalIgnoreCase))
//        {
//            spec.Kind = TargetKind.Self;
//            return spec;
//        }

//        if (token.Equals("all".AsSpan(), StringComparison.OrdinalIgnoreCase))
//        {
//            spec.Kind = TargetKind.All;
//            return spec;
//        }

//        int dot = token.IndexOf('.');

//        if (dot > 0)
//        {
//            var left = token[..dot];
//            var right = token[(dot + 1)..];

//            if (left.Equals("all".AsSpan(), StringComparison.OrdinalIgnoreCase))
//            {
//                spec.Kind = TargetKind.All;
//                spec.Name = right;
//                return spec;
//            }

//            if (int.TryParse(left, out int index))
//            {
//                spec.Kind = TargetKind.Indexed;
//                spec.Index = index;
//                spec.Name = right;
//                return spec;
//            }
//        }

//        spec.Kind = TargetKind.Single;
//        spec.Name = token;

//        return spec;
//    }

//    private ref struct ArgumentEnumerator
//    {
//        private ReadOnlySpan<char> _remaining;

//        public ArgumentEnumerator(ReadOnlySpan<char> args)
//        {
//            _remaining = args.Trim();
//            Current = default;
//        }

//        public ReadOnlySpan<char> Current { get; private set; }

//        public bool MoveNext()
//        {
//            if (_remaining.IsEmpty)
//                return false;

//            int space = _remaining.IndexOf(' ');

//            if (space < 0)
//            {
//                Current = _remaining;
//                _remaining = default;
//                return true;
//            }

//            Current = _remaining[..space];
//            _remaining = _remaining[(space + 1)..].TrimStart();

//            return true;
//        }
//    }
//}
