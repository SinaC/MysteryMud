using System.Net;
using System.Net.Sockets;

namespace MysteryMud.ConsoleApp3.Network;

public class TelnetServer
{
    private readonly TcpListener _listener;
    private readonly Dictionary<int, TelnetSession> _sessions = new(); // TODO: needed ?
    private readonly Action<int, ReadOnlySpan<char>> OnCommand;
    private readonly Action<int, ReadOnlySpan<char>> OnLogin;

    private int _nextConnectionId;

    public TelnetServer(int port, Action<int, ReadOnlySpan<char>> onCommand, Action<int, ReadOnlySpan<char>> onLogin)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        OnCommand = onCommand;
        OnLogin = onLogin;
    }

    public async Task Start()
    {
        _listener.Start();
        Console.WriteLine("Server listening...");

        while (true)
        {
            var tcpClient = await _listener.AcceptSocketAsync();

            var connectionId = _nextConnectionId;
            _nextConnectionId++;

            // create TelnetConnection
            var conn = new TelnetSession(tcpClient, connectionId, OnCommand, OnLogin);

            _sessions[connectionId] = conn;

            // start processing the player connection
            _ = conn.Start();

            Console.WriteLine("New player connected.");
        }
    }

    public void SendGMCP(int connectionId, string package, object payload)
    {
        // TODO: handle case where connectionId is not found (e.g. player disconnected)
        _sessions[connectionId].SendGMCP(package, payload);
    }

    public void Flush(int connectionId)
    {
        // TODO: handle case where connectionId is not found (e.g. player disconnected)
        _sessions[connectionId].Flush();
    }

    public void Write(int connectionId, string text)
    {
        // TODO: handle case where connectionId is not found (e.g. player disconnected)
        _sessions[connectionId].Write(text);
    }

    public void Write(int connectionId, ReadOnlySpan<char> span)
    {
        // TODO: handle case where connectionId is not found (e.g. player disconnected)
        _sessions[connectionId].Write(span);
    }
}
