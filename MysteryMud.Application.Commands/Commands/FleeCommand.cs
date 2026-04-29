using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class FleeCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public FleeCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (!actor.Has<CombatState>())
        {
            _msg.To(actor).Send("You aren't fighting anyone.");
            return;
        }

        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // intent to flee
        ref var intent = ref _intents.Flee.Add();
        intent.Entity = actor;
        intent.FromRoom = room;
    }
}
