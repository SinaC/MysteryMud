using MysteryMud.Core.Effects;

namespace MysteryMud.Domain.Action.Attack.Factories;

public interface IHitDamageFactory
{
    DamageAction CreateHitDamage(AttackResult attackResult);
}