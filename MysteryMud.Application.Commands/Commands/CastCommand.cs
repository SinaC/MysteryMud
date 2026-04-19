using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.Commands;

public class CastCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public CastCommand(IAbilityRegistry abilityRegistry, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _abilityRegistry = abilityRegistry;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // search ability
        if (!_abilityRegistry.StartsWith(ctx.Primary.Name.ToString(), out var abilityRuntime) || abilityRuntime == null || abilityRuntime.Kind != AbilityKind.Spell)
        {
            _msg.To(actor).Send("You don't know any spells of that name.");
            return;
        }
        var abilityId = abilityRuntime.Id;

        // checks will be done in AbilityValidationSystem

        // add use ability intent
        ref var useAbilityIntent = ref _intents.UseAbility.Add();
        useAbilityIntent.Source = actor;
        useAbilityIntent.TargetKind = ctx.Secondary.Kind;
        useAbilityIntent.TargetIndex = ctx.Secondary.Index;
        useAbilityIntent.TargetName = ctx.Secondary.Name.ToString();
        useAbilityIntent.AbilityId = abilityId;
        useAbilityIntent.Cancelled = false;
    }
}
