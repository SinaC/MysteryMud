using MysteryMud.Core.Eventing;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Combat.Factories;

public class DamageFactory
{
    private readonly IEventBuffer<DamageEvent> _damages;

    public DamageFactory(IEventBuffer<DamageEvent> damages)
    {
        _damages = damages;
    }

    public ref DamageEvent Create(AttackResolved resolved) // no need to check if source/target is alive
    {
        //ref var effectiveStats = ref resolved.Source.Get<EffectiveStats>();
        // TODO: calculate damage based on stats, skills, buffs, etc.
        // TODO: calculate damage type based on weapon

        ref var damageEvt = ref _damages.Add();
        damageEvt.Source = resolved.Source;
        damageEvt.Target = resolved.Target;
        damageEvt.Amount = 5; // TODO: calculate damage based on stats, skills, buffs, etc.
        damageEvt.DamageType = DamageTypes.Physical;
        damageEvt.SourceType = DamageSourceTypes.Hit;

        return ref damageEvt;
    }
}
