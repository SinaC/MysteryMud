using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Application.Commands.Parser;
using MysteryMud.Application.Systems;

namespace MysteryMud.Application.Commands;

public class CastCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair; // could have 3 parameters ?

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // search spell
        var spellName = ctx.Primary.Name;
        if (!SpellSystem.SpellDatabase.Spells.TryGetValue(spellName.ToString(), out var spell))
        {
            systemContext.MessageBus.Publish(actor, $"Unknown spell: {spellName}");
            return;
        }

        // search target
        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents);
        if (target == default)
        {
            systemContext.MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        SpellSystem.CastSpell(systemContext, gameState, actor, target, spell);
    }
}
