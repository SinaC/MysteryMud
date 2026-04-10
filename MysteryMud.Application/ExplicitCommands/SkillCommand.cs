using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Queries;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class SkillCommand : IExplicitCommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly ILogger _logger;
    private readonly AbilityRegistry _abilityRegistry;

    public CommandDefinition Definition { get; }

    public SkillCommand(ILogger logger, AbilityRegistry abilityRegistry, CommandDefinition definition)
    {
        _logger = logger;
        _abilityRegistry = abilityRegistry;

        Definition = definition;
    }

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // search ability (should always be found because SkillCommands are generated from abilities)
        if (!_abilityRegistry.StartsWith(Definition.Name, out var abilityRuntime) || abilityRuntime == null || abilityRuntime.Kind != AbilityKind.Skill)
        {
            executionContext.Msg.To(actor).Send("You don't know any skills of that name.");
            return;
        }
        var abilityId = abilityRuntime.Id;

        // TODO: check arguments depending on skill

        // TODO: check resource/cooldown/position/...
        ref var casting = ref actor.TryGetRef<Casting>(out var isCasting);
        if (isCasting)
        {
            if (!_abilityRegistry.TryGetValue(casting.AbilityId, out var castingAbilityRuntime) || castingAbilityRuntime == null)
            {
                _logger.LogError("{actorName} is focused on an unknown ability {abilityId}", actor.DebugName, casting.AbilityId);
                actor.Remove<Casting>(); // remove casting and allow to cast a new spell
            }
            else
            {
                executionContext.Msg.To(actor).Send($"You are already focused on {castingAbilityRuntime.Name}");
                return;
            }
        }

        // add ability intent
        ref var useAbilityIntent = ref executionContext.Intent.UseAbility.Add();
        useAbilityIntent.Source = actor;
        useAbilityIntent.TargetKind = ctx.Primary.Kind;
        useAbilityIntent.TargetIndex = ctx.Primary.Index;
        useAbilityIntent.TargetName = ctx.Primary.Name.ToString();
        useAbilityIntent.AbilityId = abilityId;
        useAbilityIntent.AbilityKind = abilityRuntime.Kind;
    }
}
