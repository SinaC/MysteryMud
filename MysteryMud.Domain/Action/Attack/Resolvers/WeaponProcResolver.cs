using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class WeaponProcResolver
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;
    private readonly IWeaponProcRegistry _weaponProcRegistry;
    private readonly IEffectRegistry _effectRegistry;

    public WeaponProcResolver(ILogger logger, IGameMessageService msg, IIntentWriterContainer intents, IWeaponProcRegistry weaponProcRegistry, IEffectRegistry effectRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _weaponProcRegistry = weaponProcRegistry;
        _effectRegistry = effectRegistry;
    }

    public void Resolve(GameState state, AttackResult attack, DamageResult damageResult)
    {
        if (!CharacterHelpers.IsAlive(attack.Source, attack.Target))
            return;

        // search weapon
        ref var equipment = ref attack.Source.TryGetRef<Equipment>(out var hasEquipment);
        if (!hasEquipment)
            return;
        if (!equipment.Slots.TryGetValue(EquipmentSlotKind.MainHand, out var weaponEntity) || weaponEntity == default || weaponEntity == Entity.Null)
            return;
        ref var weapon = ref weaponEntity.TryGetRef<Weapon>(out var isWeapon);
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
                _logger.LogWarning("WeaponProcResolver: weapon {weaponName} has unknown proc {weaponProcId}", weaponEntity.DebugName, weaponProcId);
                continue;
            }

            // TODO: chance
            // TODO: message ?

            foreach (var weaponProcEffect in weaponProcRuntime.WeaponProcEffectRuntimes)
            {
                // existing effect ?
                if (!_effectRegistry.TryGetRuntime(weaponProcEffect.EffectId, out var effectRuntime) || effectRuntime == null)
                {
                    _logger.LogWarning("WeaponProcResolver: weapon {weaponName} tried to proc effect {effectId}", weaponEntity.DebugName, weaponProcEffect.EffectId);
                    continue;
                }

                Entity target = weaponProcEffect.Target switch
                {
                    WeaponProcTarget.Opponent => attack.Target,
                    WeaponProcTarget.Self => attack.Source,
                    _ => attack.Target,
                };

                if (!CharacterHelpers.IsAlive(attack.Source, target))
                    continue;

                _msg.ToAll(attack.Source).Act("{0} apply{0:v} effect {1} to {2}.").With(weaponEntity, effectRuntime.Name, target);

                ref var effectIntent = ref _intents.Action.Add();
                effectIntent.Kind = ActionKind.Effect;
                effectIntent.Effect.EffectId = effectRuntime.Id;
                effectIntent.Effect.Source = attack.Source; // TODO: weaponEntity ?
                effectIntent.Effect.Target = target;
                effectIntent.Effect.EffectiveDamageAmount = damageResult.EffectiveAmount;
                effectIntent.Cancelled = false;
            }
        }
    }
}
