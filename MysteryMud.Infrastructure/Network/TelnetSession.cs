using MysteryMud.Infrastructure.Buffers;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace MysteryMud.Infrastructure.Network;

public sealed class TelnetSession : IDisposable
{
    private const int MAX_TTYPE_REQUESTS = 5;

    private readonly Socket _socket;
    private readonly Pipe _pipe = new();
    private readonly Channel<bool> _sendSignal = Channel.CreateUnbounded<bool>();
    private readonly OutputBuffer _output = new();

    private ZLibStream? _compressor;


    private readonly int ConnectionId;

    internal TelnetState TelnetState;
    private int _ttypeRequests = 0;
    private bool _disposed;

    private readonly Action<int, ReadOnlySpan<char>> OnInputReceived;
    private readonly Action<int> OnConnected;
    private readonly Action<int> OnDisconnected;

    public TelnetSession(Socket socket, int connectionId, Action<int, ReadOnlySpan<char>> onInputReceived, Action<int> onConnected, Action<int> onDisconnected)
    {
        _socket = socket;
        ConnectionId = connectionId;
        OnInputReceived = onInputReceived;
        OnConnected = onConnected;
        OnDisconnected = onDisconnected;

        TelnetState = new TelnetState();
    }

    public async Task Start()
    {
        try
        {
            SendInitialNegotiation();

            EchoOn();

            OnConnected(ConnectionId);

            var receive = ReceiveLoop();
            var parse = ParseLoop();
            var send = SendLoop();

            await Task.WhenAny(receive, parse, send);
        }
        finally
        {
            // Ensure proper cleanup
            Dispose();

            // Notify TelnetServer/GameServer
            OnDisconnected?.Invoke(ConnectionId);
        }
    }

    // ----------------------------------------------------
    // RECEIVE LOOP
    // ----------------------------------------------------

    private async Task ReceiveLoop()
    {
        var writer = _pipe.Writer;

        try
        {
            while (!_disposed)
            {
                Memory<byte> memory = writer.GetMemory(4096);

                int bytes = await _socket.ReceiveAsync(memory);

                if (bytes == 0)
                    break;

                writer.Advance(bytes);

                var result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    //Console.WriteLine("Output stream completed.");
                    break;
                }
            }
        }
        catch (Exception)
        {
            //Console.WriteLine($"Error in ReceiveLoop: {ex}");
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
            while (!_disposed)
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
                    //Console.WriteLine("Input stream completed.");
                    break;
                }
            }
        }
        catch (Exception)
        {
            //Console.WriteLine($"Error in ParseLoop: {ex}");
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

        //
        OnInputReceived?.Invoke(ConnectionId, span);

        ArrayPool<char>.Shared.Return(buffer);
    }

    // ----------------------------------------------------
    // OUTPUT WRITING
    // ----------------------------------------------------

    public void Write(string text)
    {
        if (_disposed)
            return;

        Write(text.AsSpan());
    }

    //    public void Write(ReadOnlySpan<char> span)
    //    {
    //        if (_disposed)
    //            return;

    //        if (!span.ContainsAny('%', '{'))
    //        {
    //            while (!span.IsEmpty)
    //            {
    //#pragma warning disable CA2014 // Do not use stackalloc in loops
    //                Span<byte> temp = stackalloc byte[1024];
    //#pragma warning restore CA2014 // Do not use stackalloc in loops

    //                Encoding.ASCII.GetEncoder().Convert(
    //                    span,
    //                    temp,
    //                    flush: false,
    //                    out var charsUsed,
    //                    out var bytesUsed,
    //                    out var completed);

    //                WriteBytes(temp[..bytesUsed]);
    //                span = span[charsUsed..];
    //            }
    //        }

    //        MudColorPipeline.Render(this, span);
    //    }

    //public void Write(ReadOnlySpan<char> span)
    //{
    //    if (_disposed || span.IsEmpty)
    //        return;

    //    // If the string is simple (no color codes / % or {), encode directly
    //    if (!span.ContainsAny('%', '{'))
    //    {
    //        WriteAsciiDirect(span);
    //        return;
    //    }

    //    // Otherwise, use your MudColorPipeline
    //    MudColorPipeline.Render(this, span);
    //}

    public void Write(ReadOnlySpan<char> span)
    {
        if (_disposed || span.IsEmpty)
            return;

        // --- FAST PATH: no IAC, no color sequences ---
        if (!span.Contains((char)Telnet.IAC) && !span.ContainsAny('%', '{'))
        {
            _output.Write(MemoryMarshal.Cast<char, byte>(span));
            return;
        }

        // --- SLOW PATH: rare IAC or color sequences ---
        int start = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            // Escape IAC byte
            if ((byte)c == Telnet.IAC)
            {
                // Write chunk before IAC
                if (i > start)
                    _output.Write(MemoryMarshal.Cast<char, byte>(span.Slice(start, i - start)));

                // Double the IAC
                _output.WriteByte(Telnet.IAC);
                _output.WriteByte(Telnet.IAC);

                start = i + 1;
                continue;
            }

            // Handle color sequences
            if (c == '%' || c == '{')
            {
                // Write chunk before the color code
                if (i > start)
                    _output.Write(MemoryMarshal.Cast<char, byte>(span.Slice(start, i - start)));

                // Hand off remaining span to the color pipeline
                MudColorPipeline.Render(this, span[i..]);
                return;
            }
        }

        // Write any remaining chunk
        if (start < span.Length)
            _output.Write(MemoryMarshal.Cast<char, byte>(span.Slice(start)));
    }

    internal void WriteChar(char c)
    {
        if (_disposed)
            return;

        byte b = (byte)c; // ASCII only

        if (b == Telnet.IAC)
        {
            // Escape IAC
            _output.WriteByte(Telnet.IAC);
            _output.WriteByte(Telnet.IAC);
        }
        else
        {
            _output.WriteByte(b);
        }
    }

    internal void WriteAnsi(ReadOnlySpan<char> text)
    {
        if (_disposed || text.IsEmpty)
            return;

        // --- FAST PATH: no IAC ---
        _output.Write(MemoryMarshal.Cast<char, byte>(text));
    }

    internal void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_disposed || bytes.IsEmpty)
            return;

        // IAC is rare, check for it first to avoid unnecessary looping and method calls in the common case
        if (!bytes.Contains(Telnet.IAC))
        {
            _output.Write(bytes);
            return;
        }

        // IAC is present, need to escape it by doubling
        int start = 0;

        while (true)
        {
            // Find the next IAC byte
            int idx = bytes[start..].IndexOf(Telnet.IAC);

            if (idx < 0)
            {
                // No more IAC → write remaining slice in one go
                _output.Write(bytes[start..]);
                return;
            }

            idx += start;

            // Write chunk before IAC
            if (idx > start)
            {
                _output.Write(bytes[start..idx]);
            }

            // Escape IAC by doubling it
            _output.WriteByte(Telnet.IAC);
            _output.WriteByte(Telnet.IAC);

            // Move past the escaped byte
            start = idx + 1;

            if (start >= bytes.Length)
                return;
        }
    }

    public void Flush()
    {
        if (_disposed)
            return;

        _sendSignal.Writer.TryWrite(true);
    }

    // ----------------------------------------------------
    // SEND LOOP
    // ----------------------------------------------------

    private async Task SendLoop()
    {
        await foreach (var _ in _sendSignal.Reader.ReadAllAsync())
        {
            if (_disposed)
                break;

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
    // ECHO
    // =====================================================
    public void EchoOn()
    {
        SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO); // Enable local echo
    }

    public void EchoOff()
    {
        SendCmd(Telnet.WILL, Telnet.TELOPT_ECHO); // Disable local echo for password input
    }

    // =====================================================
    // GMCP
    // =====================================================

    public void SendGMCP(string package, object payload)
    {
        if (_disposed || !TelnetState.Gmcp)
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

    private void SendInitialNegotiation()
    {
        SendCmd(Telnet.WILL, Telnet.TELOPT_SGA);
        SendCmd(Telnet.WILL, Telnet.TELOPT_ECHO);
        SendCmd(Telnet.WILL, Telnet.TELOPT_GMCP);
        SendCmd(Telnet.WILL, Telnet.TELOPT_MCCP2);
        SendCmd(Telnet.DO, Telnet.TELOPT_SGA);
        SendCmd(Telnet.DO, Telnet.TELOPT_TTYPE);
        SendCmd(Telnet.DO, Telnet.TELOPT_NAWS);
    }

    private bool TryConsume(ref SequenceReader<byte> reader)
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

    private void HandleNegotiation(byte cmd, byte opt)
    {
        // DEBUG
        //Console.WriteLine($"CMD: 0x{cmd:X2} 0x{opt:X2}");

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

        //Console.WriteLine($"SB: 0x{option:X2}");

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

    private void RequestSendTerminalType()
    {
        Span<byte> b = stackalloc byte[]
        {
            Telnet.IAC, Telnet.SB, Telnet.TELOPT_TTYPE,
            Telnet.ENV_SEND,
            Telnet.IAC, Telnet.SE
        };
        _output.Write(b); // directly write to output buffer to ensure proper escaping of IAC bytes
    }

    private void RequestSendNaw()
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
        _ttypeRequests++;
        if (_ttypeRequests < MAX_TTYPE_REQUESTS)
            RequestSendTerminalType();

        return true;
    }

    private void ParseMTTS(string value)
    {
        var parts = value.Split(' ');
        if (parts.Length != 2)
            return;

        if (!int.TryParse(parts[1], out int flags))
            return;

        // flag bits (commonly used)
        const int MTTS_ANSI = 1;
        //const int MTTS_UTF8 = 4;
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

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _compressor?.Dispose();
        }
        catch { }

        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
            _socket?.Dispose();
            _output.Dispose();
        }
        catch { }

        _pipe.Writer.Complete();
        _pipe.Reader.Complete();
        _sendSignal.Writer.Complete();

        // null out references to help GC
        _compressor = null!;
    }
}
