using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Services;

namespace MysteryMud.Domain.Systems;

public sealed class DisconnectSystem
{
    private readonly IConnectionService _connections;
    private readonly IIntentContainer _intents;

    public DisconnectSystem(IConnectionService connections, IIntentContainer intents)
    {
        _connections = connections;
        _intents = intents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.DisconnectSpan)
        {
            var player = intent.Player;
            _connections.Disconnect(player);
            // DisconnectedTag will be set by GameServer.HandleDisconnected
            // which fires from the socket close callback
        }
    }
}
