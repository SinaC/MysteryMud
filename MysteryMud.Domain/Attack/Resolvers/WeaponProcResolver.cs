using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Effect;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Attack.Resolvers;

public class WeaponProcResolver
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;
    private readonly EffectRegistry _effectRegistry;

    //public struct WeaponProc
    //{
    //    public SkillEffect Effect;   // Could be Damage, Heal, Buff, Debuff, etc.
    //    public float Chance;         // 0..1 probability
    //}

    public WeaponProcResolver(ILogger logger, IGameMessageService msg, IIntentWriterContainer intents, EffectRegistry effectRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _effectRegistry = effectRegistry;
    }

    public void Resolve(GameState state, AttackResult attack)
    {
        if (!CharacterHelpers.IsAlive(attack.Target))
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

        // proc ?
        if (weapon.ProcEffectId is null) // TODO: loop on effect + apply chance
            return;

        // existing effect ?
        if (!_effectRegistry.TryGetValue(weapon.ProcEffectId.Value, out var effectRuntime) || effectRuntime == null)
        {
            _logger.LogWarning("WeaponProcResolver: weapon {weaponName} tried to proc effect {effectId}", weaponEntity.DebugName, weapon.ProcEffectId);
            return;
        }

        _msg.ToAll(attack.Source).Act("{0} apply{0:v} effect {1} to {2}.").With(weaponEntity, effectRuntime.Name, attack.Target);

        ref var effectIntent = ref _intents.Effect.Add();
        effectIntent.EffectId = weapon.ProcEffectId.Value;
        effectIntent.Source = attack.Source; // TODO: weaponEntity ?
        effectIntent.Target = attack.Target;
    }
}
