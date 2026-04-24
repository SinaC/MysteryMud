using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class WeaponProcResolver : IWeaponProcResolver
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IRandom _random;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;
    private readonly IWeaponProcRegistry _weaponProcRegistry;
    private readonly IEffectRegistry _effectRegistry;

    public WeaponProcResolver(World world, ILogger logger, IRandom random, IGameMessageService msg, IIntentWriterContainer intents, IWeaponProcRegistry weaponProcRegistry, IEffectRegistry effectRegistry)
    {
        _world = world;
        _logger = logger;
        _random = random;
        _msg = msg;
        _intents = intents;
        _weaponProcRegistry = weaponProcRegistry;
        _effectRegistry = effectRegistry;
    }

    public void Resolve(GameState state, AttackResult attack, DamageResult damageResult)
    {
        var attackSource = attack.Source;
        var attackTarget = attack.Target;

        if (!CharacterHelpers.IsAlive(_world, attackSource, attackTarget))
            return;

        // search weapon
        ref var equipment = ref _world.TryGetRef<Equipment>(attackSource, out var hasEquipment);
        if (!hasEquipment)
            return;
        if (!equipment.Slots.TryGetValue(EquipmentSlotKind.MainHand, out var weaponEntity) || weaponEntity == default || weaponEntity == EntityId.Invalid)
            return;
        ref var weapon = ref _world.TryGetRef<Weapon>(weaponEntity, out var isWeapon);
        if (!isWeapon)
            return;

        // has proc ?
        if (weapon.ProcIds == null || weapon.ProcIds.Count == 0)
            return;
        foreach (var weaponProcId in weapon.ProcIds)
        {
            // existing weapon proc ?
            if (!_weaponProcRegistry.TryGetRuntime(weaponProcId, out var weaponProcRuntime) || weaponProcRuntime == null)
            {
                _logger.LogWarning("WeaponProcResolver: weapon {weaponName} has unknown proc {weaponProcId}", EntityHelpers.DebugName(_world, weaponEntity), weaponProcId);
                continue;
            }

            var trigger = _random.Chance(weaponProcRuntime.Chance);
            if (!trigger)
                continue;

            foreach (var weaponProcEffect in weaponProcRuntime.WeaponProcEffectRuntimes)
            {
                // existing effect ?
                if (!_effectRegistry.TryGetRuntime(weaponProcEffect.EffectId, out var effectRuntime) || effectRuntime == null)
                {
                    _logger.LogWarning("WeaponProcResolver: weapon {weaponName} tried to proc effect {effectId}", EntityHelpers.DebugName(_world, weaponEntity), weaponProcEffect.EffectId);
                    continue;
                }

                var target = weaponProcEffect.Target switch
                {
                    WeaponProcTarget.Opponent => attackTarget,
                    WeaponProcTarget.Self => attackSource,
                    _ => attackTarget,
                };

                if (!CharacterHelpers.IsAlive(_world, attackSource, target))
                    continue;

                _msg.ToAll(attackSource).Act("{0} apply{0:v} effect {1} to {2}.").With(weaponEntity, effectRuntime.Name, target);

                ref var effectIntent = ref _intents.Action.Add();
                effectIntent.Kind = ActionKind.Effect;
                effectIntent.Effect.EffectId = effectRuntime.Id;
                effectIntent.Effect.Source = attackSource; // TODO: weaponEntity ?
                effectIntent.Effect.Target = target;
                effectIntent.Effect.IsHarmful = true;
                effectIntent.Effect.EffectiveDamageAmount = damageResult.EffectiveAmount;
                effectIntent.Cancelled = false;
            }
        }
    }
}
