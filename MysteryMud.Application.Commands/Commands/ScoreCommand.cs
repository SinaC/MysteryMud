using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public class ScoreCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IEffectDisplayService _effectDisplayService;

    public ScoreCommand(World world, IGameMessageService msg, IEffectDisplayService effectDisplayService)
    {
        _world = world;
        _msg = msg;
        _effectDisplayService = effectDisplayService;
    }

    public void Execute( GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var name = ref _world.Get<Name>(actor);
        ref var baseStats = ref _world.Get<BaseStats>(actor);
        ref var effectiveStats = ref _world.Get<EffectiveStats>(actor);
        ref var characterEffects = ref _world.Get<CharacterEffects>(actor);
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
        ref var combatState = ref _world.TryGetRef<CombatState>(actor, out var inCombat);
        if (inCombat)
            _msg.To(actor).Act("Fighting: {0}").With(combatState.Target);

        _msg.To(actor).Send($"Active tags: {characterEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, characterEffects.Data.Effects);
    }

    private void DisplayLevelAndExperience(EntityId actor)
    {
        ref var level = ref _world.TryGetRef<Level>(actor, out var hasLevel);
        if (!hasLevel)
            return;
        _msg.To(actor).Send($"Level: {level.Value}");
        ref var progression = ref _world.TryGetRef<Progression>(actor, out var hasProgression);
        if (hasProgression)
            _msg.To(actor).Send($"Experience: {progression.Experience} NextLevel: {progression.ExperienceToNextLevel - progression.Experience} XP");
    }

    private void DisplayHealth(EntityId actor)
    {
        var health = _world.Get<Health>(actor);
        _msg.To(actor).Send($"Health: {health.Current}/{health.Max}");
    }

    private void DisplayMove(EntityId actor)
    {
        var move = _world.Get<Move>(actor);
        _msg.To(actor).Send($"Move: {move.Current}/{move.Max}");
    }

    private void DisplayResource<TResource, TRegen, TUses>(EntityId actor, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        var uses = _world.Has<TUses>(actor);
        if (!uses)
            return;
        ref var resource = ref _world.TryGetRef<TResource>(actor, out var hasResource);
        if (hasResource)
        {
            var (current, max) = getCurrentMaxFunc(resource);
            _msg.To(actor).Send($"{kind}: {current}/{max} CanUse: {uses}");
        }
    }
}
