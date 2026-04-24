using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class MstatCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IEffectDisplayService _effectDisplayService;

    public MstatCommand(World world, IGameMessageService msg, IEffectDisplayService effectDisplayService)
    {
        _world = world;
        _msg = msg;
        _effectDisplayService = effectDisplayService;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Mstat what ?");
            return;
        }

        ref var room = ref _world.Get<Location>(actor).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;

        var target = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Primary, people);
        if (target == null)
        {
            _msg.To(actor).Send("No such target.");
            return;
        }

        ref var name = ref _world.Get<Name>(actor);
        ref var location = ref _world.Get<Location>(actor);
        ref var baseStats = ref _world.Get<BaseStats>(actor);
        ref var effectiveStats = ref _world.Get<EffectiveStats>(actor);
        ref var inventory = ref _world.Get<Inventory>(actor);
        ref var equipment = ref _world.Get<Equipment>(actor);
        ref var characterEffects = ref _world.Get<CharacterEffects>(actor);
        _msg.To(actor).Send($"Name: {name.Value}");
        ref var description = ref _world.TryGetRef<Description>(target.Value, out var hasDescription);
        if (hasDescription)
            _msg.To(actor).Send($"Description: {description.Value}");
        _msg.To(actor).Send($"Location: {EntityHelpers.DebugName(_world, location.Room)}");
        DisplayHealth(actor, target.Value);
        DisplayMove(actor, target.Value);
        DisplayResource<Mana, ManaRegen, UsesMana>(actor, target.Value, ResourceKind.Mana, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        DisplayResource<Energy, EnergyRegen, UsesEnergy>(actor, target.Value, ResourceKind.Energy, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        DisplayResource<Rage, RageDecay, UsesRage>(actor, target.Value, ResourceKind.Rage, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        foreach (var stat in Enum.GetValues<CharacterStatKind>().Take((int)CharacterStatKind.Count))
        {
            _msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref _world.TryGetRef<CombatState>(target.Value, out var inCombat);
        if (inCombat)
            _msg.To(actor).Send($"Fighting: {EntityHelpers.DebugName(_world, combatState.Target)} Delay: {combatState.RoundDelay}");
        _msg.To(actor).Send($"Inventory:");
        foreach (var item in inventory.Items)
            _msg.To(actor).Send($"- {EntityHelpers.DebugName(_world, item)}");
        _msg.To(actor).Send($"Equipment:");
        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                _msg.To(actor).Send($"{slot}: {EntityHelpers.DebugName(_world, item)}");
            else
                _msg.To(actor).Send($"{slot}: nothing");
        }
        _msg.To(actor).Send($"Active tags: {characterEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, characterEffects.Data.Effects);
    }

    private void DisplayHealth(EntityId actor, EntityId target)
    {
        var health = _world.Get<Health>(target);
        ref var healthRegen = ref _world.TryGetRef<HealthRegen>(target, out var hasRegen);
        var currentRegen = hasRegen
            ? healthRegen.CurrentAmountPerSecond
            : 0;
        var baseRegen = hasRegen
            ? healthRegen.BaseAmountPerSecond
            : 0;
        _msg.To(actor).Send($"Health: {health.Current}/{health.Max} Regen: {currentRegen}(base: {baseRegen})");
    }

    private void DisplayMove(EntityId actor, EntityId target)
    {
        var move = _world.Get<Move>(target);
        ref var moveRegen = ref _world.TryGetRef<MoveRegen>(target, out var hasRegen);
        var currentRegen = hasRegen
            ? moveRegen.CurrentAmountPerSecond
            : 0;
        var baseRegen = hasRegen
            ? moveRegen.BaseAmountPerSecond
            : 0;
        _msg.To(actor).Send($"Move: {move.Current}/{move.Max} Regen: {currentRegen}(base: {baseRegen})");
    }

    private void DisplayResource<TResource, TRegen, TUses>(EntityId actor, EntityId target, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc, Func<TRegen, int> getCurrentRegenFunc, Func<TRegen, int> getBaseRegenFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        ref var resource = ref _world.TryGetRef<TResource>(target, out var hasResource);
        if (hasResource)
        {
            var (current, max) = getCurrentMaxFunc(resource);
            ref var resourceRegen = ref _world.TryGetRef<TRegen>(target, out var hasRegen);
            var currentRegen = hasRegen
                ? getCurrentRegenFunc(resourceRegen)
                : 0;
            var baseRegen = hasRegen
                ? getBaseRegenFunc(resourceRegen)
                : 0;
            var uses = _world.Has<TUses>(target);
            _msg.To(actor).Send($"{kind}: {current}/{max} Regen/Decay: {currentRegen}(base: {baseRegen}) CanUse: {uses}");
        }
    }
}
