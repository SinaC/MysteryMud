using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using MysteryMud.ConsoleApp3.Commands;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Networking;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Systems;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace MysteryMud.ConsoleApp3.Network;

public sealed class TelnetConnection
{
    private const int MAX_TTYPE_REQUESTS = 5;

    private readonly Socket _socket;
    private readonly Pipe _pipe = new();
    private readonly Channel<bool> _sendSignal = Channel.CreateUnbounded<bool>();

    private readonly OutputBuffer _output = new();

    private ZLibStream _compressor = default!;

    public TelnetState TelnetState;
    public Entity Player;

    private int ttypeRequests = 0;

    public NannyState NannyState = NannyState.NewConnection;
    private string _tempName = default!;
    private string _tempPassword = default!;

    public TelnetConnection(Socket socket, Entity player)
    {
        _socket = socket;
        TelnetState = new TelnetState();
        Player = player;
    }

    public async Task Start()
    {
        SendInitialNegotiation();

        HandleNanny(null);

        var receive = ReceiveLoop();
        var parse = ParseLoop();
        var send = SendLoop();

        await Task.WhenAny(receive, parse, send);
    }

    // ----------------------------------------------------
    // RECEIVE LOOP
    // ----------------------------------------------------

    private async Task ReceiveLoop()
    {
        var writer = _pipe.Writer;

        try
        {
            while (true)
            {
                Memory<byte> memory = writer.GetMemory(4096);

                int bytes = await _socket.ReceiveAsync(memory);

                if (bytes == 0)
                    break;

                writer.Advance(bytes);

                var result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    Console.WriteLine("Output stream completed.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ReceiveLoop: {ex}");
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }

    // ----------------------------------------------------
    // PARSE LOOP
    // ----------------------------------------------------

    private async Task ParseLoop()
    {
        var reader = _pipe.Reader;

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                var seqReader = new SequenceReader<byte>(buffer);

                while (true)
                {
                    if (TryConsume(ref seqReader))
                        continue;

                    if (TryReadLine(ref seqReader, out var line))
                        HandleLine(line);
                    else
                        break;
                }

                reader.AdvanceTo(seqReader.Position, buffer.End);

                if (result.IsCompleted)
                {
                    Console.WriteLine("Input stream completed.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ParseLoop: {ex}");
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    private static bool TryReadLine(
        ref SequenceReader<byte> reader,
        out ReadOnlySequence<byte> line)
    {
        if (reader.TryReadTo(out line, (byte)'\n'))
            return true;

        line = default;
        return false;
    }

    private void HandleLine(ReadOnlySequence<byte> line)
    {
        // trim CR
        if (line.Length > 0 &&
            line.Slice(line.Length - 1).FirstSpan[0] == '\r')
        {
            line = line.Slice(0, line.Length - 1);
        }

        DecodeAndQueue(line);
    }

    private void DecodeAndQueue(ReadOnlySequence<byte> line)
    {
        int len = (int)line.Length;

        char[] buffer = ArrayPool<char>.Shared.Rent(len);

        int written = 0;

        if (line.IsSingleSegment)
        {
            written = Encoding.ASCII.GetChars(
                line.FirstSpan,
                buffer);
        }
        else
        {
            foreach (var segment in line)
            {
                written += Encoding.ASCII.GetChars(
                    segment.Span,
                    buffer.AsSpan(written));
            }
        }

        var span = buffer.AsSpan(0, written);

        if (NannyState != NannyState.Finished)
            HandleNanny(span);
        else
            CommandSystem.Enqueue(Player, span);

        ArrayPool<char>.Shared.Return(buffer);
    }

    // ----------------------------------------------------
    // OUTPUT WRITING
    // ----------------------------------------------------

    public void Write(string text)
    {
        if (!text.AsSpan().ContainsAny('%', '{'))
        {
            WriteRaw(text);
            return;
        }

        MudColorPipeline.Render(this, text.AsSpan());
    }

    public void WriteRaw(string text)
    {
        Span<byte> temp = stackalloc byte[1024];

        int bytes = Encoding.ASCII.GetBytes(text, temp);

        WriteBytes(temp[..bytes]);
    }

    public void WriteChar(char c)
    {
        Span<char> cbuf = stackalloc char[1];
        Span<byte> bbuf = stackalloc byte[4];

        cbuf[0] = c;

        int bytes = Encoding.ASCII.GetBytes(cbuf, bbuf);

        WriteBytes(bbuf[..bytes]);
    }

    public void WriteAnsi(ReadOnlySpan<char> text)
    {
        Span<byte> buf = stackalloc byte[64];

        int bytes = Encoding.ASCII.GetBytes(text, buf);

        WriteBytes(buf[..bytes]);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            if (b == Telnet.IAC)
            {
                _output.WriteByte(Telnet.IAC);
                _output.WriteByte(Telnet.IAC);
            }
            else
            {
                _output.WriteByte(b);
            }
        }
    }

    public void Flush()
    {
        _sendSignal.Writer.TryWrite(true);
    }

    // ----------------------------------------------------
    // SEND LOOP
    // ----------------------------------------------------

    private async Task SendLoop()
    {
        await foreach (var _ in _sendSignal.Reader.ReadAllAsync())
        {
            var data = _output.Data;

            if (data.Length == 0)
                continue;

            if (TelnetState.Mccp && _compressor != null)
            {
                await _compressor.WriteAsync(data);
                await _compressor.FlushAsync();
            }
            else
            {
                await SendAll(_socket, data);
            }

            _output.Clear();
        }
    }

    private static async Task SendAll(
        Socket socket,
        ReadOnlyMemory<byte> buffer)
    {
        int sent = 0;

        while (sent < buffer.Length)
        {
            int bytes = await socket.SendAsync(
                buffer[sent..],
                SocketFlags.None);

            if (bytes == 0)
                throw new SocketException();

            sent += bytes;
        }
    }

    // =====================================================
    // GMCP
    // =====================================================

    public void SendGMCP(string package, object payload)
    {
        if (!TelnetState.Gmcp)
            return;

        string json = JsonSerializer.Serialize(payload);
        string message = $"{package} {json}";

        byte[] data = Encoding.UTF8.GetBytes(message);

        SendCmd(Telnet.SB, Telnet.TELOPT_GMCP);
        _output.Write(data);
        SendCmd(Telnet.SE, 0);

        Flush(); // Ensure GMCP message is sent immediately
    }

    // =====================================================
    // MCCP
    // =====================================================

    private void StartMccp()
    {
        if (TelnetState.Mccp)
            return;

        _output.Write(
        [
            Telnet.IAC,
            Telnet.SB,
            Telnet.TELOPT_MCCP2,
            Telnet.IAC,
            Telnet.SE
        ]);
        Flush(); // Ensure the negotiation commands are sent before starting compression

        _compressor = new ZLibStream(
            new NetworkStream(_socket, true),
            CompressionMode.Compress,
            true
        );

        TelnetState.Mccp = true;
    }

    // ----------------------------------------------------
    // TELNET NEGOTIATION
    // ----------------------------------------------------
    //Understanding Telnet Negotiation
    //  Telnet is not just “send text”; it has a control protocol for option negotiation:
    //  IAC(Interpret As Command) – 0xFF
    //  Commands:
    //      DO(0xFD) – Ask client to enable option
    //      DONT(0xFE) – Ask client to disable option
    //      WILL(0xFB) – Server wants to enable option
    //      WONT(0xFC) – Server refuses or disables option
    //  Options are single-byte identifiers:
    //      MCCP2: 0x56 (sub-option for Telnet Compression)
    //      GMCP: 0xC9 (201 in decimal, defined as generic MUD communication protocol)
    //  So the server negotiation flow is roughly:
    //  Server sends IAC WILL MCCP2 → client responds IAC DO MCCP2
    //  Server sends IAC WILL GMCP → client responds IAC DO GMCP
    //  After MCCP2 is agreed, server starts compressing output.
    //  GMCP allows structured JSON - like messages to be exchanged.

    private void SendCmd(byte cmd, byte opt)
    {
        Span<byte> b = stackalloc byte[]
        {
            Telnet.IAC,
            cmd,
            opt
        };
        _output.Write(b); // directly write to output buffer to ensure proper escaping of IAC bytes
    }

    void SendInitialNegotiation()
    {
        SendCmd(Telnet.WILL, Telnet.TELOPT_SGA);
        SendCmd(Telnet.WILL, Telnet.TELOPT_ECHO);
        SendCmd(Telnet.WILL, Telnet.TELOPT_GMCP);
        SendCmd(Telnet.WILL, Telnet.TELOPT_MCCP2);
        SendCmd(Telnet.DO, Telnet.TELOPT_SGA);
        SendCmd(Telnet.DO, Telnet.TELOPT_TTYPE);
        SendCmd(Telnet.DO, Telnet.TELOPT_NAWS);
    }

    bool TryConsume(ref SequenceReader<byte> reader)
    {
        if (!reader.TryPeek(out byte b) || b != Telnet.IAC)
            return false;

        reader.Advance(1);

        if (!reader.TryRead(out byte cmd))
            return false;

        switch (cmd)
        {
            case Telnet.WILL: // WILL
            case Telnet.WONT: // WONT
            case Telnet.DO: // DO
            case Telnet.DONT: // DONT
                if (reader.TryRead(out byte opt))
                    HandleNegotiation(cmd, opt);
                return true;

            case Telnet.SB:
                return HandleSubNegotiation(ref reader);
        }

        return true;
    }

    void HandleNegotiation(byte cmd, byte opt)
    {
        // DEBUG
        Console.WriteLine($"CMD: 0x{cmd:X2} 0x{opt:X2}");

        switch (cmd)
        {
            case Telnet.WILL:
                if (opt == Telnet.TELOPT_TTYPE)
                    RequestSendTerminalType();
                else if (opt == Telnet.TELOPT_NAWS)
                    RequestSendNaw();
                else
                    SendCmd(Telnet.DO, opt);
                break;
            case Telnet.WONT:
                SendCmd(Telnet.DONT, opt);
                break;
            case Telnet.DO:
                if (opt == Telnet.TELOPT_GMCP)
                    TelnetState.Gmcp = true;
                else if (opt == Telnet.TELOPT_MCCP2)
                    StartMccp();
                // TODO: ECHO ?
                break;
            case Telnet.DONT:
                if (opt == Telnet.TELOPT_GMCP)
                    TelnetState.Gmcp = false;
                if (opt == Telnet.TELOPT_MCCP2)
                    TelnetState.Mccp = false;
                break;
        }
    }

    private bool HandleSubNegotiation(ref SequenceReader<byte> reader)
    {
        if (!reader.TryRead(out byte option))
            return false;

        Console.WriteLine($"SB: 0x{option:X2}");

        switch (option)
        {
            case Telnet.TELOPT_NAWS:
                return ParseNaws(ref reader);

            case Telnet.TELOPT_TTYPE:
                return ParseTerminal(ref reader);
        }

        SkipSub(ref reader);
        return true;
    }

    void RequestSendTerminalType()
    {
        Span<byte> b = stackalloc byte[]
        {
            Telnet.IAC, Telnet.SB, Telnet.TELOPT_TTYPE,
            Telnet.ENV_SEND,
            Telnet.IAC, Telnet.SE
        };
        _output.Write(b); // directly write to output buffer to ensure proper escaping of IAC bytes
    }

    void RequestSendNaw()
    {
        Span<byte> b = stackalloc byte[]
        {
            Telnet.IAC, Telnet.SB, Telnet.TELOPT_NAWS,
            Telnet.ENV_SEND,
            Telnet.IAC, Telnet.SE
        };
        _output.Write(b); // directly write to output buffer to ensure proper escaping of IAC bytes
    }

    private bool ParseNaws(ref SequenceReader<byte> reader)
    {
        Span<byte> buf = stackalloc byte[4];

        if (!reader.TryCopyTo(buf))
            return false;

        reader.Advance(4);

        TelnetState.Width = BinaryPrimitives.ReadUInt16BigEndian(buf);
        TelnetState.Height = BinaryPrimitives.ReadUInt16BigEndian(buf[2..]);

        SkipSub(ref reader);
        return true;
    }

    private bool ParseTerminal(ref SequenceReader<byte> reader)
    {
        var name = new ArrayBufferWriter<byte>();

        while (reader.TryRead(out byte b))
        {
            if (b == Telnet.IAC)
            {
                reader.TryRead(out _);
                break;
            }

            if (b == Telnet.ENV_IS)
                continue;
            name.Write([b]);
        }

        var terminal = Encoding.ASCII.GetString(name.WrittenSpan).ToLowerInvariant();
        // determine color mode
        if (terminal.StartsWith("mtts"))
            ParseMTTS(terminal);
        else if (terminal.Contains("truecolor"))
            TelnetState.ColorMode = ColorMode.TrueColor;
        else if (terminal.Contains("256") && TelnetState.ColorMode < ColorMode.ANSI256)
            TelnetState.ColorMode = ColorMode.ANSI256;
        else if ((terminal.Contains("xterm") || terminal.Contains("ansi")) && TelnetState.ColorMode < ColorMode.ANSI16)
            TelnetState.ColorMode = ColorMode.ANSI16;
        if (!TelnetState.Terminals.Contains(terminal))
            TelnetState.Terminals.Add(terminal);

        // request terminal type again to get more info (some clients only send generic "xterm" first, then more specific on subsequent requests)
        ttypeRequests++;
        if (ttypeRequests < MAX_TTYPE_REQUESTS)
            RequestSendTerminalType();

        return true;
    }

    void ParseMTTS(string value)
    {
        var parts = value.Split(' ');
        if (parts.Length != 2)
            return;

        if (!int.TryParse(parts[1], out int flags))
            return;

        // flag bits (commonly used)
        const int MTTS_ANSI = 1;
        const int MTTS_UTF8 = 4;
        const int MTTS_256COLOR = 8;
        const int MTTS_TRUECOLOR = 256;

        if ((flags & MTTS_ANSI) != 0)
            TelnetState.ColorMode = ColorMode.ANSI16;

        if ((flags & MTTS_256COLOR) != 0)
            TelnetState.ColorMode = ColorMode.ANSI256;

        if ((flags & MTTS_TRUECOLOR) != 0)
            TelnetState.ColorMode = ColorMode.TrueColor;
    }

    private void SkipSub(ref SequenceReader<byte> reader)
    {
        while (reader.TryRead(out byte b))
        {
            if (b == Telnet.IAC &&
                reader.TryPeek(out byte next) &&
                next == Telnet.SE)
            {
                reader.Advance(1);
                break;
            }
        }
    }


    // ----------------------------------------------------
    // NANNY
    // ----------------------------------------------------
    // TODO: refactor
    private void HandleNanny(ReadOnlySpan<char> input)
    {
        _tempName = "joel";
        SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO);
        NannyState = NannyState.Finished;
        InitializePlayer();
        return;

        // TODO

        switch (NannyState)
        {
            case NannyState.NewConnection:
                SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO); // Enable local echo
                Write("Welcome to the MUD!\r\nPlease enter your name: ");
                Flush();
                NannyState = NannyState.EnterName;
                break;

            case NannyState.EnterName:
                // COLOR TESTING
                //ColorTest(ColorMode.TrueColor);
                //ColorTest(ColorMode.ANSI256);
                //ColorTest(ColorMode.ANSI16);
                //ColorTest(ColorMode.None);
                //ColorTest();

                _tempName = input.ToString().Trim();
                Write("Enter your password: ");
                SendCmd(Telnet.WILL, Telnet.TELOPT_ECHO); // Disable local echo for password input
                Flush();
                NannyState = NannyState.EnterPassword;
                break;

            case NannyState.EnterPassword:
                _tempPassword = input.ToString();
                // Here you could verify password from a database or file
                Write("Confirm password: ");
                Flush();
                NannyState = NannyState.ConfirmPassword;
                break;

            case NannyState.ConfirmPassword:
                if (_tempPassword != input.ToString())
                {
                    Write("Passwords do not match. Enter password: ");
                    Flush();
                    NannyState = NannyState.EnterPassword;
                }
                else
                {
                    Write("Character creation complete!\r\n");
                    SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO); // Enable local echo
                    Flush();
                    NannyState = NannyState.Finished;

                    // Attach default ECS components
                    InitializePlayer();
                }
                break;
        }
    }

    //private void ColorTest(ColorMode colorMode)
    //{
    //    TelnetState.ColorMode = colorMode;
    //    Write($"=={colorMode}==\r\n");
    //    ColorTest();
    //    Flush();
    //}

    //private void ColorTest()
    //{
    //    Write("Ansi16: %RR%GG%YY%BB%MM%CC%WW%rr%gg%yy%bb%mm%cc%ww%xnocolor\r\n");
    //    Write("Ansi256: %=214orange%xnocolor\r\n");
    //    Write("RGB: %#FFA500orange%xnocolor\r\n");
    //    Write("GRADIENT: %#FFA500>#00FFA5orange-2-cyan%xnocolor\r\n");
    //}

    private void InitializePlayer()
    {
        // TODO: fill in with actual character creation data, load from file, etc
        Player.Add(new PlayerTag());
        Player.Add(new Name { Value = _tempName });
        Player.Add(new BaseStats
        {
            Level = 1,
            Experience = 0,
            Values = new Dictionary<StatType, int>
            {
                [StatType.Strength] = 15,
                [StatType.Intelligence] = 10,
                [StatType.Wisdom] = 15,
                [StatType.Dexterity] = 12,
                [StatType.Constitution] = 15,
                [StatType.HitRoll] = 0,
                [StatType.DamRoll] = 0,
                [StatType.Armor] = 0
            }
        });
        Player.Add(new EffectiveStats
        {
            Values = new Dictionary<StatType, int>
            {
                [StatType.Strength] = 0,
                [StatType.Intelligence] = 0,
                [StatType.Wisdom] = 0,
                [StatType.Dexterity] = 0,
                [StatType.Constitution] = 0,
                [StatType.HitRoll] = 0,
                [StatType.DamRoll] = 0,
                [StatType.Armor] = 0
            }
        });
        Player.Add(new Health { Current = 100, Max = 100 });
        Player.Add(new Inventory { Items = [] });
        Player.Add(new Equipment { Slots = [] });
        Player.Add(new CharacterEffects
        {
            Effects = [],
            EffectsByTag = new Entity?[32]
        });
        Player.Add(new Position { Room = WorldFactory.StartingRoomEntity });
        Player.Add<DirtyStats>(); // ensure stats are recomputed
        WorldFactory.StartingRoomEntity.Get<RoomContents>().Characters.Add(Player);

        Write("Welcome to the game!\r\n> ");
        Flush();
    }
}
