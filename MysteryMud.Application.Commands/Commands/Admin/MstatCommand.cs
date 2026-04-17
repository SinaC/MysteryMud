using Arch.Core;
using Arch.Core.Extensions;
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

public class MstatCommand : ICommand
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

        // TODO: ref ?
        var (name, location, baseStats, effectiveStats, inventory, equipment, characterEffects) = target.Value.Get<Name, Location, BaseStats, EffectiveStats, Inventory, Equipment, CharacterEffects>();
        _msg.To(actor).Send($"Name: {name.Value}");
        ref var description = ref target.Value.TryGetRef<Description>(out var hasDescription);
        if (hasDescription)
            _msg.To(actor).Send($"Description: {description.Value}");
        _msg.To(actor).Send($"Location: {location.Room.DisplayName}");
        DisplayHealth(actor, target.Value);
        DisplayResource<Mana, ManaRegen, UsesMana>(actor, target.Value, ResourceKind.Mana, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        DisplayResource<Energy, EnergyRegen, UsesEnergy>(actor, target.Value, ResourceKind.Energy, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        DisplayResource<Rage, RageDecay, UsesRage>(actor, target.Value, ResourceKind.Rage, x => (x.Current, x.Max), x => x.CurrentAmountPerSecond, x => x.CurrentAmountPerSecond);
        foreach (var stat in Enum.GetValues<CharacterStatKind>().Take((int)CharacterStatKind.Count))
        {
            _msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref target.Value.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            _msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
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
        ref var healthRegen = ref target.TryGetRef<HealthRegen>(out var hasRegen);
        var currentRegen = hasRegen
            ? healthRegen.CurrentAmountPerSecond
            : 0;
        var baseRegen = hasRegen
            ? healthRegen.BaseAmountPerSecond
            : 0;
        _msg.To(actor).Send($"Health: {health.Current}/{health.Max} Regen: {currentRegen}(base: {baseRegen})");
    }

    private void DisplayResource<TResource, TRegen, TUses>(Entity actor, Entity target, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc, Func<TRegen, int> getCurrentRegenFunc, Func<TRegen, int> getBaseRegenFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        ref var resource = ref target.TryGetRef<TResource>(out var hasResource);
        if (hasResource)
        {
            var (current, max) = getCurrentMaxFunc(resource);
            ref var resourceRegen = ref target.TryGetRef<TRegen>(out var hasRegen);
            var currentRegen = hasRegen
                ? getCurrentRegenFunc(resourceRegen)
                : 0;
            var baseRegen = hasRegen
                ? getBaseRegenFunc(resourceRegen)
                : 0;
            var uses = target.Has<TUses>();
            _msg.To(actor).Send($"{kind}: {current}/{max} Regen/Decay: {currentRegen}(base: {baseRegen}) CanUse: {uses}");
        }
    }
}
