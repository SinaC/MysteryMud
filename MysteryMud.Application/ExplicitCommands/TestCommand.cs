using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Effect;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class TestCommand : ICommand
{
    private const string Name = "test";

    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly EffectRegistry _effectRegistry;

    public CommandDefinition Definition { get; }

    public TestCommand(EffectRegistry effectRegistry)
    {
        _effectRegistry = effectRegistry;

        Definition = new CommandDefinition
        {
            Id = Name.ComputeUniqueId(),
            Name = Name,
            Aliases = [],
            CannotBeForced = true,
            RequiredLevel = CommandLevelKind.Admin,
            MinimumPosition = PositionKind.Dead,
            Priority = 0,
            AllowAbbreviation = false,
            HelpText = "",
            Syntaxes = ["[cmd]"],
            Categories = ["test"],
            ThrottlingCategories = CommandThrottlingCategories.Admin
        };
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents);

        //systemContext.Msg.To(actor).Send("Ansi16: %RR%GG%YY%BB%MM%CC%WW%rr%gg%yy%bb%mm%cc%ww%xnocolor");
        //systemContext.Msg.To(actor).Send("Ansi256: %=214orange%xnocolor");
        //systemContext.Msg.To(actor).Send("RGB: %#FFA500orange%xnocolor");
        //systemContext.Msg.To(actor).Send("GRADIENT: %#FFA500>#00FFA5orange-2-cyan%xnocolor");

        if (target == default)
        {
            systemContext.Msg.To(actor).Send("You don't see that here.");
            return;
        }

        // search effect and add effect intent if found
        int? effectId = null;
        if (_effectRegistry.TryGetValue(ctx.Text.ToString(), out var effectRuntime) && effectRuntime != null)
            effectId = effectRuntime.Id;
        if (effectId is not null && effectRuntime is not null)
        {
            systemContext.Msg.To(actor).Send($"Applying effect {effectRuntime.Name}");
            ref var effectIntent = ref systemContext.Intent.Effect.Add();
            effectIntent.EffectId = effectId.Value;
            effectIntent.Source = actor;
            effectIntent.Target = target;
        }

        // TODO: search spell and add ability intent if found
    }
}
