using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.DataDrivenCommands;

public sealed class SkillCommand : IExplicitCommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public CommandDefinition Definition { get; }

    public SkillCommand(World world, ILogger logger, IAbilityRegistry abilityRegistry, IGameMessageService msg, IIntentWriterContainer intents, CommandDefinition definition)
    {
        _world = world;
        _logger = logger;
        _abilityRegistry = abilityRegistry;
        _msg = msg;
        _intents = intents;

        Definition = definition;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // search ability (should always be found because SkillCommands are generated from abilities)
        var startsWithResult = _abilityRegistry.StartsWith(ctx.Primary.Name.ToString(), out var abilityRuntime);
        if (startsWithResult == Core.Utilities.StartsWithResult.NotFound
            || (abilityRuntime != null && abilityRuntime.Kind != AbilityKind.Skill))
        {
            _msg.To(actor).Send("You don't know any skills of that name.");
            return;
        }
        if (startsWithResult == Core.Utilities.StartsWithResult.Ambiguous)
        {
            _msg.To(actor).Send("You know multiple skills with that name.");
            return;
        }
        if (abilityRuntime == null)
        {
            _msg.To(actor).Send("Something goes wrong!");
            _logger.LogError("Skill: '{skillName}' returns a null ability runtime", ctx.Primary.Name.ToString());
            return;
        }

        var abilityId = abilityRuntime.Id;

        // TODO: check arguments depending on skill

        // TODO: check resource/cooldown/position/...
        ref var casting = ref _world.TryGetRef<Casting>(actor, out var isCasting);
        if (isCasting)
        {
            if (!_abilityRegistry.TryGetRuntime(casting.AbilityId, out var castingAbilityRuntime) || castingAbilityRuntime == null)
            {
                _logger.LogError("{actorName} is focused on an unknown ability {abilityId}", EntityHelpers.DebugName(_world, actor), casting.AbilityId);
                _world.Remove<Casting>(actor); // remove casting and allow to cast a new spell
            }
            else
            {
                _msg.To(actor).Send($"You are already focused on {castingAbilityRuntime.Name}");
                return;
            }
        }

        // add ability intent
        ref var useAbilityIntent = ref _intents.UseAbility.Add();
        useAbilityIntent.Source = actor;
        useAbilityIntent.TargetKind = ctx.Primary.Kind;
        useAbilityIntent.TargetIndex = ctx.Primary.Index;
        useAbilityIntent.TargetName = ctx.Primary.Name.ToString();
        useAbilityIntent.AbilityId = abilityId;
        useAbilityIntent.Cancelled = false;
    }
}
