using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.ExplicitCommands.Admin;

public class TestCommand : IExplicitCommand
{
    private const string Name = "test";

    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly IEffectRegistry _effectRegistry;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public CommandDefinition Definition { get; }

    public TestCommand(IEffectRegistry effectRegistry, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _effectRegistry = effectRegistry;
        _msg = msg;
        _intents = intents;

        Definition = new CommandDefinition
        {
            Id = Name.ComputeUniqueId(),
            Name = Name,
            Aliases = [],
            CannotBeForced = true,
            RequiredLevel = CommandLevelKind.Admin,
            MinimumPosition = PositionKind.Dead,
            Priority = 0,
            DisallowAbbreviation = true,
            HelpText = "",
            Syntaxes = ["[cmd]"],
            Categories = ["test"],
            ThrottlingCategories = CommandThrottlingCategories.Admin
        };
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = CommandEntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents);

        //_msg.To(actor).Send("Ansi16: %RR%GG%YY%BB%MM%CC%WW%rr%gg%yy%bb%mm%cc%ww%xnocolor");
        //_msg.To(actor).Send("Ansi256: %=214orange%xnocolor");
        //_msg.To(actor).Send("RGB: %#FFA500orange%xnocolor");
        //_msg.To(actor).Send("GRADIENT: %#FFA500>#00FFA5orange-2-cyan%xnocolor");

        if (target == null)
        {
            _msg.To(actor).Send("You don't see that here.");
            return;
        }

        // search effect and add effect intent if found
        if (_effectRegistry.TryGetRuntime(ctx.Text.ToString(), out var effectRuntime) && effectRuntime != null)
        {
            var effectId = effectRuntime.Id;

            _msg.To(actor).Send($"Applying effect {effectRuntime.Name}");
            ref var effectIntent = ref _intents.Action.Add();
            effectIntent.Kind = ActionKind.Effect;
            effectIntent.Effect.EffectId = effectId;
            effectIntent.Effect.Source = actor;
            effectIntent.Effect.Target = target.Value;
            effectIntent.Cancelled = false;
            return;
        }

        // TODO: search spell and add ability intent if found
    }
}
