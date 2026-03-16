using System.Runtime.CompilerServices;

namespace MysteryMud.ConsoleApp3.Network;

struct MudColorDef
{
    public byte R;
    public byte G;
    public byte B;
}

public static class MudColorPipeline
{
    private const char ColorEscapeChar = '%';

    static MudColorPipeline()
    {
        // Precompute the Ansi256 to Ansi16 mapping
        BuildAnsi256ToColorCodeMap();
        // Precompute hex table for fast hex parsing
        CreateHexTable();
        // Precompute digit table for fast decimal parsing
        CreateDecimalTable();
    }

    public static void Render(TelnetConnection conn, ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            // Handle escaped %%
            if (text[i] == ColorEscapeChar && i + 1 < text.Length && text[i + 1] == ColorEscapeChar)
            {
                conn.WriteChar(ColorEscapeChar);
                i++;
                continue;
            }

            // If it's not a %, just write it and move on
            if (text[i] != ColorEscapeChar)
            {
                conn.WriteChar(text[i]);
                continue;
            }

            // Check for gradient %#AABBCC>#DDEEFF
            if (TryParseGradient(text, i,
                out var a, out var b, out var gcons))
            {
                i += gcons;

                int start = i;
                while (i < text.Length && text[i] != ColorEscapeChar)
                    i++;

                WriteGradient(conn,
                    text[start..i],
                    a, b);

                i--;
                continue;
            }

            // Check for RGB %#RRGGBB
            if (TryParseRgb(text, i,
                out var rgb, out var cons))
            {
                WriteColor(conn, rgb);

                i += cons - 1;
                continue;
            }

            // check for %=###
            if (TryParse256(text, i,
                out var paletteIndex, out var cons256))
            {
                Write256(conn, paletteIndex);

                i += cons256 - 1;
                continue;
            }

            // Check for color code %r, %g, etc.
            if (TryParseColorCode(text, i,
                out var colorCode, out var cc))
            {
                Write16(conn, colorCode);

                i += cc - 1;
                continue;
            }

            conn.WriteChar(text[i]);
        }
    }

    private static string Fg16(int c) => $"\x1b[{c}m";
    private static string Fg256(int c) => $"\x1b[38;5;{c}m";
    private static string FgRgb(byte r, byte g, byte b) => $"\x1b[38;2;{r};{g};{b}m";

    static void Write16(TelnetConnection conn, int colorCode)
    {
        if (conn.TelnetState.ColorMode == ColorMode.None)
            return;
        conn.WriteAnsi(Fg16(colorCode));
    }

    static void Write256(TelnetConnection conn, int paletteIndex)
    {
        switch (conn.TelnetState.ColorMode)
        {
            case ColorMode.TrueColor:
            case ColorMode.ANSI256:
                conn.WriteAnsi(Fg256(paletteIndex));
                break;

            case ColorMode.ANSI16:
                var colorCode = Ansi256ToColorCode[paletteIndex];
                conn.WriteAnsi(Fg16(colorCode));
                break;
        }
    }

    static void WriteColor(TelnetConnection conn, MudColorDef c)
    {
        switch (conn.TelnetState.ColorMode)
        {
            case ColorMode.TrueColor:
                conn.WriteAnsi(FgRgb(c.R, c.G, c.B));
                break;

            case ColorMode.ANSI256:
                int paletteIndex = RgbToAnsi256(c.R, c.G, c.B);
                conn.WriteAnsi(Fg256(paletteIndex));
                break;

            case ColorMode.ANSI16:
                var colorCode = RgbToAnsi16(c.R, c.G, c.B);
                conn.WriteAnsi(Fg16(colorCode));

                break;
        }
    }

    static void WriteGradient(TelnetConnection conn, ReadOnlySpan<char> text, MudColorDef a, MudColorDef b)
    {
        int len = text.Length;

        for (int i = 0; i < len; i++)
        {
            float t = i / (float)(len - 1);

            var c = Lerp(a, b, t);

            WriteColor(conn, c);

            conn.WriteChar(text[i]);
        }
    }

    static bool TryParseColorCode(ReadOnlySpan<char> text, int i, out int color, out int consumed)
    {
        color = default;
        consumed = 0;

        if (i + 1 >= text.Length)
            return false;

        if (text[i] != ColorEscapeChar)
            return false;

        color = text[i + 1] switch
        {
            // reset
            'x' => 39,
            'X' => 39,

            // bright colors
            'R' => 91,
            'G' => 92,
            'Y' => 93,
            'B' => 94,
            'M' => 95,
            'C' => 96,
            'W' => 97,

            // normal colors
            'r' => 31,
            'g' => 32,
            'y' => 33,
            'b' => 34,
            'm' => 35,
            'c' => 36,
            'w' => 37,

            _ => -1
        };

        if (color == -1)
            return false;

        consumed = 2;
        return true;
    }

    private static bool TryParse256(ReadOnlySpan<char> text, int i, out int color, out int consumed)
    {
        color = default;
        consumed = 0;

        if (i + 5 >= text.Length)
            return false;

        if (text[i] != ColorEscapeChar || text[i + 1] != '=')
            return false;

        // no validation, just parse hex digits
        color = DecimalByte(text[i + 2], text[i + 3], text[i + 4]);

        consumed = 5; // %=XXX
        return true;
    }

    static bool TryParseRgb(ReadOnlySpan<char> text, int i, out MudColorDef color, out int consumed)
    {
        color = default;
        consumed = 0;

        if (i + 8 >= text.Length)
            return false;

        if (text[i] != ColorEscapeChar || text[i + 1] != '#')
            return false;

        // no validation, just parse hex digits
        color.R = HexByte(text[i + 2], text[i + 3]);
        color.G = HexByte(text[i + 4], text[i + 5]);
        color.B = HexByte(text[i + 6], text[i + 7]);

        consumed = 8; // %#RRGGBB
        return true;
    }

    static bool TryParseGradient(ReadOnlySpan<char> text, int i, out MudColorDef a, out MudColorDef b, out int consumed)
    {
        a = default;
        b = default;
        consumed = 0;

        if (i + 16 >= text.Length)
            return false;

        if (text[i] != ColorEscapeChar || text[i + 1] != '#')
            return false;

        var arrow = text[i + 8];

        if (arrow != '>')
            return false;

        if (text[i + 9] != '#')
            return false;

        // no validation, just parse hex digits
        a.R = HexByte(text[i + 2], text[i + 3]);
        a.G = HexByte(text[i + 4], text[i + 3]);
        a.B = HexByte(text[i + 6], text[i + 3]);

        b.R = HexByte(text[i + 10], text[i + 11]);
        b.G = HexByte(text[i + 12], text[i + 13]);
        b.B = HexByte(text[i + 14], text[i + 15]);

        consumed = 16; // %#AABBCC>#DDEEFF
        return true;
    }

    private static readonly byte[] Ansi256ToColorCode = new byte[256];

    private static void BuildAnsi256ToColorCodeMap()
    {
        for (int i = 0; i < 256; i++)
            Ansi256ToColorCode[i] = Ansi256ToAnsi16(i);
    }

    static readonly (int r, int g, int b)[] Ansi16 =
    [
        (0,0,0),       // 0 black
        (128,0,0),     // 1 red
        (0,128,0),     // 2 green
        (128,128,0),   // 3 yellow
        (0,0,128),     // 4 blue
        (128,0,128),   // 5 magenta
        (0,128,128),   // 6 cyan
        (192,192,192), // 7 white
    
        (128,128,128), // 8 bright black (gray)
        (255,0,0),     // 9 bright red
        (0,255,0),     // 10 bright green
        (255,255,0),   // 11 bright yellow
        (0,0,255),     // 12 bright blue
        (255,0,255),   // 13 bright magenta
        (0,255,255),   // 14 bright cyan
        (255,255,255)  // 15 bright white
    ];

    //https://github.com/fidian/ansi
    static int RgbToAnsi256(byte r, byte g, byte b)
    {
        int ir = r * 5 / 255;
        int ig = g * 5 / 255;
        int ib = b * 5 / 255;

        return 16 + 36 * ir + 6 * ig + ib;
    }

    private static byte RgbToAnsi16(int r, int g, int b)
    {
        byte bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (byte i = 0; i < Ansi16.Length; i++)
        {
            var (ar, ag, ab) = Ansi16[i];

            int dr = r - ar;
            int dg = g - ag;
            int db = b - ab;

            int dist = dr * dr + dg * dg + db * db;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestIndex = i;
            }
        }
        // from here we have an index in Ansi16 table
        // indices 0-7 are normal colors, 8-15 are bright colors
        // we want to map them to 30-37 for normal and 90-97 for bright

        if (bestIndex < 8)
            bestIndex += 30; // normal colors
        else
            bestIndex += 90-8; // bright colors
        return bestIndex;
    }

    private static readonly int[] CubeLevels = [0, 95, 135, 175, 215, 255];

    private static (int r, int g, int b) Ansi256ToRgb(int index)
    {
        if (index < 16)
            return Ansi16[index];

        if (index <= 231)
        {
            int i = index - 16;

            int r = i / 36;
            int g = (i % 36) / 6;
            int b = i % 6;

            return (CubeLevels[r], CubeLevels[g], CubeLevels[b]);
        }

        int gray = 8 + (index - 232) * 10;
        return (gray, gray, gray);
    }

    private static byte Ansi256ToAnsi16(int index)
    {
        var (r, g, b) = Ansi256ToRgb(index);
        return RgbToAnsi16(r, g, b);
    }

    static MudColorDef Lerp(MudColorDef a, MudColorDef b, float t)
    {
        return new MudColorDef
        {
            R = (byte)(a.R + (b.R - a.R) * t),
            G = (byte)(a.G + (b.G - a.G) * t),
            B = (byte)(a.B + (b.B - a.B) * t)
        };
    }

    // this will 'parse' 3 decimal digits from the input, but it will not validate that they are actually digits
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte DecimalByte(char hi, char mid, char lo)
        => (byte)(DecimalTable[hi] * 100 + DecimalTable[mid] * 10 + DecimalTable[lo]);

    static readonly byte[] DecimalTable = new byte[256];

    static void CreateDecimalTable()
    {
        for (int i = 0; i < DecimalTable.Length; i++)
            DecimalTable[i] = 0; // by default, no mapping

        for (int i = 0; i <= 9; i++)
            DecimalTable['0' + i] = (byte)i;
    }

    // this will 'parse' 2 hex digits from the input, but it will not validate that they are actually hex digits
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte HexByte(char hi, char lo)
        => (byte)((HexTable[hi] << 4) | HexTable[lo]);

    static readonly byte[] HexTable = new byte[256];

    static void CreateHexTable()
    {
        for (int i = 0; i < HexTable.Length; i++)
            HexTable[i] = 0; // by default, no mapping

        for (int i = 0; i <= 9; i++)
            HexTable['0' + i] = (byte)i;

        for (int i = 0; i < 6; i++)
        {
            HexTable['A' + i] = (byte)(10 + i);
            HexTable['a' + i] = (byte)(10 + i);
        }
    }

    // if we want validation, we can use sbyte and set invalid entries to -1, but that would require more checks in the parsing code
    //static readonly sbyte[] HexTable = CreateHexTable();

    //static sbyte[] CreateHexTable()
    //{
    //    var t = new sbyte[256];

    //    for (int i = 0; i < t.Length; i++)
    //        t[i] = -1;

    //    for (int i = 0; i <= 9; i++)
    //        t['0' + i] = (sbyte)i;

    //    for (int i = 0; i < 6; i++)
    //    {
    //        t['A' + i] = (sbyte)(10 + i);
    //        t['a' + i] = (sbyte)(10 + i);
    //    }

    //    return t;
    //}

    //static bool TryHexByte(char hi, char lo, out byte value)
    //{
    //    int h = HexTable[hi];
    //    int l = HexTable[lo];

    //    if ((h | l) < 0)
    //    {
    //        value = 0;
    //        return false;
    //    }

    //    value = (byte)((h << 4) | l);
    //    return true;
    //}
}
