using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

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
        // TODO: ref ?
        var (name, baseStats, effectiveStats, characterEffects) = actor.Get<Name, BaseStats, EffectiveStats, CharacterEffects>();
        _msg.To(actor).Send($"Name: {name.Value}");
        DisplayLevelAndExperience(actor);
        DisplayHealth(actor);
        DisplayResource<Mana, ManaRegen, UsesMana>(actor, ResourceKind.Mana, x => (x.Current, x.Max));
        DisplayResource<Energy, EnergyRegen, UsesEnergy>(actor, ResourceKind.Energy, x => (x.Current, x.Max));
        DisplayResource<Rage, RageDecay, UsesRage>(actor, ResourceKind.Rage, x => (x.Current, x.Max));
        foreach (var stat in Enum.GetValues<CharacterStatKind>().Take((int)CharacterStatKind.Count))
        {
            _msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref actor.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            _msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");

        _msg.To(actor).Send($"Active tags: {characterEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, characterEffects.Data.Effects);
    }

    private void DisplayLevelAndExperience(Entity actor)
    {
        ref var level = ref actor.TryGetRef<Level>(out var hasLevel);
        if (!hasLevel)
            return;
        _msg.To(actor).Send($"Level: {level.Value}");
        ref var progression = ref actor.TryGetRef<Progression>(out var hasProgression);
        if (hasProgression)
            _msg.To(actor).Send($"Experience: {progression.Experience} NextLevel: {progression.ExperienceToNextLevel - progression.Experience} XP");
    }

    private void DisplayHealth(Entity actor)
    {
        var health = actor.Get<Health>();
        _msg.To(actor).Send($"Health: {health.Current}/{health.Max}");
    }

    private void DisplayResource<TResource, TRegen, TUses>(Entity actor, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        var uses = actor.Has<TUses>();
        if (!uses)
            return;
        ref var resource = ref actor.TryGetRef<TResource>(out var hasResource);
        if (hasResource)
        {
            var (current, max) = getCurrentMaxFunc(resource);
            _msg.To(actor).Send($"{kind}: {current}/{max} CanUse: {uses}");
        }
    }
}
