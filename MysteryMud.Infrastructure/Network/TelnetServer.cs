using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MysteryMud.Infrastructure.Network;

public class TelnetServer
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<int, TelnetSession> _sessions = new(); // TODO: needed ?
    private readonly Action<int, ReadOnlySpan<char>> OnInputReceived;
    private readonly Action<int> OnConnected;
    private readonly Action<int> OnDisconnected;

    private int _nextConnectionId;

    public TelnetServer(int port, Action<int, ReadOnlySpan<char>> onInputReceived, Action<int> onConnected, Action<int> onDisconnected)
    {
        _listener = new TcpListener(IPAddress.Any, port);

        OnInputReceived = onInputReceived;
        OnConnected = onConnected;
        OnDisconnected = onDisconnected;
    }

    public async Task Start()
    {
        _listener.Start();
        //Console.WriteLine("Server listening...");

        while (true)
        {
            var tcpClient = await _listener.AcceptSocketAsync();

            var connectionId = _nextConnectionId;
            _nextConnectionId++;

            // create TelnetConnection
            var conn = new TelnetSession(tcpClient, connectionId, OnInputReceived, OnConnected, HandleDisconnected);

            _sessions[connectionId] = conn;

            // start processing the player connection
            _ = conn.Start();

            //Console.WriteLine("New player connected.");
        }
    }

    private void HandleDisconnected(int connectionId)
    {
        // 1. remove from sessions
        // Try to remove the session in a thread-safe way
        if (_sessions.TryRemove(connectionId, out var session))
        {
            try
            {
                session.Dispose();  // safely release all resources
            }
            catch { }
        }

        // 2. notify GameServer
        OnDisconnected?.Invoke(connectionId);
    }

    public void EchoOn(int connectionId)
    {
        if (_sessions.TryGetValue(connectionId, out var session))
           session.EchoOn();
    }

    public void EchoOff(int connectionId)
    {
        if (_sessions.TryGetValue(connectionId, out var session))
           session.EchoOff();
    }

    public void SendGMCP(int connectionId, string package, object payload)
    {
        if (_sessions.TryGetValue(connectionId, out var session))
           session.SendGMCP(package, payload);
    }

    public void Write(int connectionId, string text)
    {
        if (_sessions.TryGetValue(connectionId, out var session))
           session.Write(text);
    }

    public void Write(int connectionId, ReadOnlySpan<char> span)
    {
        if (_sessions.TryGetValue(connectionId, out var session))
           session.Write(span);
    }

    public void Flush(int connectionId)
    {
        if (_sessions.TryGetValue(connectionId, out var session))
           session.Flush();
    }

    public void FlushAll()
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                session.Flush();
            }
            catch { }
        }
    }
}
