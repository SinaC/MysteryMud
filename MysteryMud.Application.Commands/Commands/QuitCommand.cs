using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class QuitCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentWriterContainer _intents;

    public QuitCommand(IGameMessageService msg, IDirtyTracker dirtyTracker, IIntentWriterContainer intents)
    {
        _msg = msg;
        _dirtyTracker = dirtyTracker;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (!actor.Has<PlayerTag>())
            return;

        _dirtyTracker.MarkDirty(actor, DirtyReason.All);
        _msg.To(actor).Send("Goodbye. Come back soon.");

        // intent to disconnect
        ref var disconnectIntent = ref _intents.Disconnect.Add();
        disconnectIntent.Player = actor;
    }
}
