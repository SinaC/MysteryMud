using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class CastCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair; // could have 3 parameters ?

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        // search spell
        var spellName = ctx.Primary.Name;
        if (!SpellSystem.SpellDatabase.Spells.TryGetValue(spellName.ToString(), out var spell))
        {
            MessageSystem.Send(actor, $"Unknown spell: {spellName}");
            return;
        }

        // search target
        var roomContents = actor.Get<Position>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents);
        if (target == default)
        {
            MessageSystem.Send(actor, "You don't see that here.");
            return;
        }

        SpellSystem.CastSpell(world, actor, target, spell);
    }
}
