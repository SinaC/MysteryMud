using Arch.Core.Extensions;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Combat.Resolvers;

public class HitResolver
{
    private IGameMessageService _msg;

    public HitResolver(IGameMessageService msg)
    {
        _msg = msg;
    }

    public AttackResolved Resolve(AttackIntent intent)
    {
        ref var stats = ref intent.Target.Get<EffectiveStats>();
        var roll = Random.Shared.NextDouble();
        AttackResults result;

        if (intent.IgnoreDefense)
            result = AttackResults.Hit;
        else
        {
            if (roll < stats.Dodge) result = AttackResults.Dodge;
            else if (roll < stats.Dodge + stats.Parry) result = AttackResults.Parry;
            else result = AttackResults.Hit;
        }

        var resolved = new AttackResolved
        {
            Source = intent.Attacker,
            Target = intent.Target,
            Result = result,
            SourceType = DamageSourceTypes.Hit
        };

        // messages
        switch (result)
        {
            case AttackResults.Dodge: _msg.ToRoom(intent.Target).Act("{0} dodges {1}'s attack.").With(intent.Target, intent.Attacker); break;
            case AttackResults.Parry: _msg.ToRoom(intent.Target).Act("{0} parries {1}'s attack.").With(intent.Target, intent.Attacker); break;
            case AttackResults.Hit: _msg.ToRoom(intent.Target).Act("{0} hits {1}.").With(intent.Attacker, intent.Target); break; // TODO: this message should probably be in DamageSystem when we know the damage amount, but we can keep it here for now for testing purposes
        }

        return resolved;
    }
}
