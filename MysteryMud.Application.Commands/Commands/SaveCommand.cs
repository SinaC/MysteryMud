using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class SaveCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;

    public SaveCommand(IGameMessageService msg, IDirtyTracker dirtyTracker)
    {
        _msg = msg;
        _dirtyTracker = dirtyTracker;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (!actor.Has<PlayerTag>())
            return;

        _dirtyTracker.MarkDirty(actor, DirtyReason.All);
        _msg.To(actor).Send("Your character has been saved.");
        // TODO: add 20 seconds lag
    }
}
