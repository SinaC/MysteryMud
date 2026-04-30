using DefaultEcs;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.Commands;

public class ScoreCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IEffectDisplayService _effectDisplayService;

    public ScoreCommand(IGameMessageService msg, IEffectDisplayService effectDisplayService)
    {
        _msg = msg;
        _effectDisplayService = effectDisplayService;
    }

    public void Execute( GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var name = ref actor.Get<Name>();
        ref var baseStats = ref actor.Get<BaseStats>();
        ref var effectiveStats = ref actor.Get<EffectiveStats>();
        ref var characterEffects = ref actor.Get<CharacterEffects>();
        _msg.To(actor).Send($"Name: {name.Value}");
        DisplayLevelAndExperience(actor);
        DisplayHealth(actor);
        DisplayMove(actor);
        DisplayResource<Mana, ManaRegen, UsesMana>(actor, ResourceKind.Mana, x => (x.Current, x.Max));
        DisplayResource<Energy, EnergyRegen, UsesEnergy>(actor, ResourceKind.Energy, x => (x.Current, x.Max));
        DisplayResource<Rage, RageDecay, UsesRage>(actor, ResourceKind.Rage, x => (x.Current, x.Max));
        foreach (var stat in Enum.GetValues<CharacterStatKind>().Take((int)CharacterStatKind.Count))
        {
            _msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        DisplayIRV(actor);
        if (actor.Has<CombatState>())
        {
            ref var combatState = ref actor.Get<CombatState>();
            _msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
        }

        _msg.To(actor).Send($"Active tags: {characterEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, characterEffects.Data.Effects);
    }

    private void DisplayLevelAndExperience(Entity actor)
    {
        if (!actor.Has<Level>())
            return;
        ref var level = ref actor.Get<Level>();
        _msg.To(actor).Send($"Level: {level.Value}");
        if (actor.Has<Progression>())
        {
            ref var progression = ref actor.Get<Progression>();
            _msg.To(actor).Send($"Experience: {progression.Experience} NextLevel: {progression.ExperienceToNextLevel - progression.Experience} XP");
        }
    }

    private void DisplayHealth(Entity actor)
    {
        var health = actor.Get<Health>();
        _msg.To(actor).Send($"Health: {health.Current}/{health.Max}");
    }

    private void DisplayMove(Entity actor)
    {
        var move = actor.Get<Move>();
        _msg.To(actor).Send($"Move: {move.Current}/{move.Max}");
    }

    private void DisplayResource<TResource, TRegen, TUses>(Entity actor, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        var uses = actor.Has<TUses>();
        if (!uses)
            return;
        if (actor.Has<TResource>())
        {
            ref var resource = ref actor.Get<TResource>();
            var (current, max) = getCurrentMaxFunc(resource);
            _msg.To(actor).Send($"{kind}: {current}/{max} CanUse: {uses}");
        }
    }

    private void DisplayIRV(Entity actor)
    {
        if (!actor.Has<EffectiveIRV>())
            return;
        ref var effectiveIRV = ref actor.Get<EffectiveIRV>();
        _msg.To(actor).Send($"Immunities: {effectiveIRV.Immunities.ToDamageKindString()} Resistances: {effectiveIRV.Resistances.ToDamageKindString()} Vulnerabilities: {effectiveIRV.Vulnerabilities.ToDamageKindString()}");
    }
}
