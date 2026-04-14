using Arch.Core.Extensions;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class HitResolver : IHitResolver
{
    private readonly IGameMessageService _msg;

    public HitResolver(IGameMessageService msg)
    {
        _msg = msg;
    }

    public AttackResult Resolve(AttackData attackData)
    {
        ref var stats = ref attackData.Target.Get<EffectiveStats>();
        var roll = Random.Shared.NextDouble();
        AttackResultKind result;

        if (attackData.IgnoreDefense)
            result = AttackResultKind.Hit;
        else
        {
            if (roll < stats.Dodge) result = AttackResultKind.Dodge;
            else if (roll < stats.Dodge + stats.Parry) result = AttackResultKind.Parry;
            else result = AttackResultKind.Hit;
        }

        var resolved = new AttackResult
        {
            Source = attackData.Source,
            Target = attackData.Target,
            Result = result
        };

        // messages
        switch (result)
        {
            case AttackResultKind.Dodge: _msg.ToAll(attackData.Target).Act("{0} dodge{0:v} {1}'s attack.").With(attackData.Target, attackData.Source); break;
            case AttackResultKind.Parry: _msg.ToAll(attackData.Target).Act("{0} parr{0:v} {1}'s attack.").With(attackData.Target, attackData.Source); break;
            case AttackResultKind.Hit: _msg.ToAll(attackData.Target).Act("{0} hit{0:v} {1}.").With(attackData.Source, attackData.Target); break; // TODO: this message should probably be in DamageSystem when we know the damage amount, but we can keep it here for now for testing purposes
        }

        return resolved;
    }
}
