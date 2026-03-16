using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Data.EffectTemplates;

public class DotTemplate : IEffectTemplate
{
    public int Damage;
    public DamageType DamageType;
    public int TickRate;

    public void Apply(World world, Entity effectEntity)
    {
        effectEntity.Add(new DamageOverTime
        {
            Damage = Damage,
            DamageType = DamageType,
            TickRate = TickRate,
            NextTick = TickRate // TODO: This is a temporary solution. We should calculate the next tick based on the current game time and the tick rate.
        });
    }
}
