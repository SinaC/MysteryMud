using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Application.Commands.Commands.Admin;

public class PeaceCommand : ICommand
{
    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        foreach (var character in actor.Get<Location>().Room.Get<RoomContents>().Characters)
        {
            character.Remove<CombatState>();
            character.Remove<NewCombatantTag>();
        }
    }
}
