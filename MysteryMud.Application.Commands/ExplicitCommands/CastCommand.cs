using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.ExplicitCommands;

public class CastCommand : IExplicitCommand
{
    private const string Name = "cast";
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly ILogger _logger;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public CommandDefinition Definition { get; }

    public CastCommand(ILogger logger, IAbilityRegistry abilityRegistry, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _logger = logger;
        _abilityRegistry = abilityRegistry;
        _msg = msg;
        _intents = intents;

        Definition = new CommandDefinition
        {
            Id = Name.ComputeUniqueId(),
            Name = Name,
            Aliases = [],
            CannotBeForced = false,
            RequiredLevel = CommandLevelKind.Player,
            MinimumPosition = PositionKind.Standing,
            Priority = 500,
            DisallowAbbreviation = false,
            HelpText = @"Before you can cast a spell, you have to practice it.  The more you practice,
the higher chance you have of success when casting.  Casting spells costs mana.
The mana cost decreases as your level increases.

The <target> is optional.  Many spells which need targets will use an
appropriate default target, especially during combat.

The <casting level> is optional. Many spells have casting level higher than 1,
and you don't have to cast spells at maximum casting level.

If the spell name is more than one word, then you must quote the spell name.
Example: cast 'cure critic' frag.  Quoting is optional for single-word spells.
You can abbreviate the spell name.

When you cast an offensive spell, the victim usually gets a saving throw.
The effect of the spell is reduced or eliminated if the victim makes the
saving throw successfully.

Some examples:
cast 'chill touch' victim   will cast 'chill touch' on victim
cast armor newbie           will cast armor on newbie
cast fireball               will cast fireball on current target if already
  in combat
cast 'floating disc'        will cast 'floating disc' on caster
See also the help sections for individual spells.
Use the 'spells' command to see the spells you already have (help spells).",
            Syntaxes = ["[cmd] <ability> <target>"],
            Categories = ["ability", "combat"],
            ThrottlingCategories = CommandThrottlingCategories.Combat
        };
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
