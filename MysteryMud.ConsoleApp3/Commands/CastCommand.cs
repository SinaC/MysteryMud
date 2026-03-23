using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class CastCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair; // could have 3 parameters ?

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        // search spell
        var spellName = ctx.Primary.Name;
        if (!SpellSystem.SpellDatabase.Spells.TryGetValue(spellName.ToString(), out var spell))
        {
            MessageBus.Publish(actor, $"Unknown spell: {spellName}");
            return;
        }

        // search target
        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents);
        if (target == default)
        {
            MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        SpellSystem.CastSpell(gameState, actor, target, spell);
    }
}
