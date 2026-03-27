using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class CastCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair; // TODO: some spells might require text argument instead of target, or no argument at all. This should be defined in CommandDefinition and handled in parsing logic
    public CommandDefinition Definition { get; }

    public CastCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Cast what ?");
        }

        // search spell
        var spellName = ctx.Primary.Name;
        if (!SpellSystem.SpellDatabase.Spells.TryGetValue(spellName.ToString(), out var spell))
        {
            systemContext.Msg.To(actor).Send($"Unknown spell: {spellName}");
            return;
        }

        // search target
        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents);
        if (target == default)
        {
            systemContext.Msg.To(actor).Send("You don't see that here.");
            return;
        }

        SpellSystem.CastSpell(systemContext, gameState, actor, target, spell);
    }
}
