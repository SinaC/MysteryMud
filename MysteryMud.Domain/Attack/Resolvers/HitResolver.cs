using Arch.Core.Extensions;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Attack.Resolvers;

public class HitResolver
{
    private IGameMessageService _msg;

    public HitResolver(IGameMessageService msg)
    {
        _msg = msg;
    }

    public AttackResult Resolve(AttackIntent intent)
    {
        ref var stats = ref intent.Target.Get<EffectiveStats>();
        var roll = Random.Shared.NextDouble();
        AttackResultKind result;

        if (intent.IgnoreDefense)
            result = AttackResultKind.Hit;
        else
        {
            if (roll < stats.Dodge) result = AttackResultKind.Dodge;
            else if (roll < stats.Dodge + stats.Parry) result = AttackResultKind.Parry;
            else result = AttackResultKind.Hit;
        }

        var resolved = new AttackResult
        {
            Source = intent.Attacker,
            Target = intent.Target,
            Result = result
        };

        // messages
        switch (result)
        {
            case AttackResultKind.Dodge: _msg.ToAll(intent.Target).Act("{0} dodge{0:v} {1}'s attack.").With(intent.Target, intent.Attacker); break;
            case AttackResultKind.Parry: _msg.ToAll(intent.Target).Act("{0} parr{0:v} {1}'s attack.").With(intent.Target, intent.Attacker); break;
            case AttackResultKind.Hit: _msg.ToAll(intent.Target).Act("{0} hit{0:v} {1}.").With(intent.Attacker, intent.Target); break; // TODO: this message should probably be in DamageSystem when we know the damage amount, but we can keep it here for now for testing purposes
        }

        return resolved;
    }
}
