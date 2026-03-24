namespace MysteryMud.Application.Commands.Parser;

class CommandParser
{
    public static void Parse(CommandParseMode parseMode, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args, out CommandContext ctx)
    {
        ctx = default;

        ctx.Command = cmd;

        switch(parseMode)
        {
            case CommandParseMode.None:
                break;
            case CommandParseMode.Target:
                if (!args.IsEmpty)
                    ctx.Primary = ParseTarget(args.Trim());
                break;
            case CommandParseMode.TargetPair:
                {
                    var e = new ArgumentEnumerator(args);

                    if (e.MoveNext())
                        ctx.Primary = ParseTarget(e.Current);

                    if (e.MoveNext())
                    {
                        var next = e.Current;

                        if (next.Equals("from".AsSpan(), StringComparison.OrdinalIgnoreCase) || next.Equals("in".AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            if (e.MoveNext())
                                ctx.Secondary = ParseTarget(e.Current);
                        }
                        else
                        {
                            ctx.Secondary = ParseTarget(next);
                        }
                    }
                    break;
                }
            case CommandParseMode.TargetAndText:
                {
                    var e = new ArgumentEnumerator(args);

                    if (e.MoveNext())
                    {
                        ctx.Primary = ParseTarget(e.Current);

                        int start = args.IndexOf(e.Current);
                        if (start >= 0)
                        {
                            var rest = args[(start + e.Current.Length)..].TrimStart();
                            ctx.Text = rest;
                        }
                    }

                    break;
                }
            case CommandParseMode.FullText:
                ctx.Text = args.Trim();
                break;
        }
    }

    public static TargetSpec ParseTarget(ReadOnlySpan<char> token)
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

    public static void SplitCommand(
        ReadOnlySpan<char> input,
        out ReadOnlySpan<char> command,
        out ReadOnlySpan<char> args)
    {
        input = input.Trim();

        int space = input.IndexOf(' ');

        if (space < 0)
        {
            command = input;
            args = ReadOnlySpan<char>.Empty;
            return;
        }

        command = input[..space];
        args = input[(space + 1)..].TrimStart();
    }

    public ref struct ArgumentEnumerator
    {
        private ReadOnlySpan<char> _remaining;

        public ArgumentEnumerator(ReadOnlySpan<char> args)
        {
            _remaining = args.Trim();
            Current = default;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
                return false;

            int space = _remaining.IndexOf(' ');

            if (space < 0)
            {
                Current = _remaining;
                _remaining = default;
                return true;
            }

            Current = _remaining[..space];
            _remaining = _remaining[(space + 1)..].TrimStart();

            return true;
        }
    }
}
