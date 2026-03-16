using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Networking;
using MysteryMud.ConsoleApp3.Factories;
using System.Net;
using System.Net.Sockets;

namespace MysteryMud.ConsoleApp3.Network;

public class TelnetServer
{
    private readonly TcpListener _listener;
    private readonly Dictionary<Entity, TelnetConnection> _connectionsByEntity = new();

    public TelnetServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task Start(World world)
    {
        _listener.Start();
        Console.WriteLine("Server listening...");

        while (true)
        {
            var tcpClient = await _listener.AcceptSocketAsync();

            // create a new ECS entity for the player
            var playerEntity = WorldFactory.CreatePlayer(world);

            // create TelnetConnection
            var conn = new TelnetConnection(tcpClient, playerEntity);

            // Attach connection component
            playerEntity.Add(new Connection { Value = conn });

            //_connections.Add(conn);
            _connectionsByEntity[playerEntity] = conn;

            // start processing the player connection
            _ = conn.Start();

            Console.WriteLine("New player connected.");
        }
    }
}
