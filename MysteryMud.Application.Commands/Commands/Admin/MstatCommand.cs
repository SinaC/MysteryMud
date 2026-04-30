using DefaultEcs;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class MstatCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IEffectDisplayService _effectDisplayService;

    public MstatCommand(IGameMessageService msg, IEffectDisplayService effectDisplayService)
    {
        _msg = msg;
        _effectDisplayService = effectDisplayService;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Mstat what ?");
            return;
        }

        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;

        var target = CommandEntityFinder.SelectSingleTarget(actor, ctx.Primary, people);
        if (target == null)
        {
            _msg.To(actor).Send("No such target.");
            return;
        }

        ref var name = ref target.Value.Get<Name>();
        ref var location = ref target.Value.Get<Location>();
        ref var baseStats = ref target.Value.Get<BaseStats>();
        ref var effectiveStats = ref target.Value.Get<EffectiveStats>();
        ref var inventory = ref target.Value.Get<Inventory>();
        ref var equipment = ref target.Value.Get<Equipment>();
        ref var characterEffects = ref target.Value.Get<CharacterEffects>();
        _msg.To(actor).Send($"Name: {name.Value}");
        if (target.Value.Has<Description>())
            _msg.To(actor).Send($"Description: {target.Value.Get<Description>().Value}");
        _msg.To(actor).Send($"Location: {location.Room.DisplayName}");
        DisplayHealth(actor, target.Value);
        DisplayMove(actor, target.Value);
        DisplayResource<Mana, ManaRegen, UsesMana>(actor, target.Value, ResourceKind.Mana, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        DisplayResource<Energy, EnergyRegen, UsesEnergy>(actor, target.Value, ResourceKind.Energy, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        DisplayResource<Rage, RageDecay, UsesRage>(actor, target.Value, ResourceKind.Rage, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        foreach (var stat in Enum.GetValues<CharacterStatKind>().Take((int)CharacterStatKind.Count))
        {
            _msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        DisplayIRV(actor, target.Value);
        if (target.Value.Has<CombatState>())
        {
            ref var combatState = ref target.Value.Get<CombatState>();
            _msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
        }
        _msg.To(actor).Send($"Inventory:");
        foreach (var item in inventory.Items)
            _msg.To(actor).Send($"- {item.DisplayName}");
        _msg.To(actor).Send($"Equipment:");
        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                _msg.To(actor).Send($"{slot}: {item.DisplayName}");
            else
                _msg.To(actor).Send($"{slot}: nothing");
        }
        _msg.To(actor).Send($"Active tags: {characterEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, characterEffects.Data.Effects);
    }

    private void DisplayHealth(Entity actor, Entity target)
    {
        var health = target.Get<Health>();
        var (currentRegen, baseRegen) = target.Has<HealthRegen>()
            ? (target.Get<HealthRegen>().CurrentAmountPerSecond, target.Get<HealthRegen>().BaseAmountPerSecond)
            : (0,0);
        _msg.To(actor).Send($"Health: {health.Current}/{health.Max} Regen: {currentRegen}(base: {baseRegen})");
    }

    private void DisplayMove(Entity actor, Entity target)
    {
        var move = target.Get<Move>();
        var (currentRegen, baseRegen) = target.Has<MoveRegen>()
            ? (target.Get<MoveRegen>().CurrentAmountPerSecond, target.Get<MoveRegen>().BaseAmountPerSecond)
            : (0, 0);
        _msg.To(actor).Send($"Move: {move.Current}/{move.Max} Regen: {currentRegen}(base: {baseRegen})");
    }

    private void DisplayResource<TResource, TRegen, TUses>(Entity actor, Entity target, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc, Func<TRegen, int> getCurrentRegenFunc, Func<TRegen, int> getBaseRegenFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        if (target.Has<TResource>())
        {
            ref var resource = ref target.Get<TResource>();
            var (current, max) = getCurrentMaxFunc(resource);
            var hasRegen = target.Has<TRegen>();
            var currentRegen = hasRegen
                ? getCurrentRegenFunc(target.Get<TRegen>())
                : 0;
            var baseRegen = hasRegen
                ? getBaseRegenFunc(target.Get<TRegen>())
                : 0;
            var uses = target.Has<TUses>();
            _msg.To(actor).Send($"{kind}: {current}/{max} Regen/Decay: {currentRegen}(base: {baseRegen}) CanUse: {uses}");
        }
    }

    private void DisplayIRV(Entity actor, Entity target)
    {
        if (!target.Has<EffectiveIRV>())
            return;
        ref var effectiveIRV = ref target.Get<EffectiveIRV>();
        _msg.To(actor).Send($"Immunities: {effectiveIRV.Immunities.ToDamageKindString()} Resistances: {effectiveIRV.Resistances.ToDamageKindString()} Vulnerabilities: {effectiveIRV.Vulnerabilities.ToDamageKindString()}");
    }
}
