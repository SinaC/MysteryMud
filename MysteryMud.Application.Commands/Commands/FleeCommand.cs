using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class FleeCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public FleeCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (!_world.Has<CombatState>(actor))
        {
            _msg.To(actor).Send("You aren't fighting anyone.");
            return;
        }

        // Get room
        ref var room = ref _world.Get<Location>(actor).Room;

        // intent to flee
        ref var intent = ref _intents.Flee.Add();
        intent.Entity = actor;
        intent.FromRoom = room;
    }
}
