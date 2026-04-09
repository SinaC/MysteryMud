using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class NotAffectedByRule : IAbilityValidationRule // opposite of AffectedByRule
{
    private readonly ulong _effectTagIndex;
    private readonly string _failMessageKey;

    public NotAffectedByRule(EffectTagId effectTagId, string failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity caster, List<Entity> targets, AbilityRuntime ability)
    {
        ref var characterEffects = ref caster.Get<CharacterEffects>();
        if ((characterEffects.ActiveTags & _effectTagIndex) != 0)
            return new()
            {
                Success = false,
                FailureMessageKey = _failMessageKey
            };

        return new()
        {
            Success = true
        };
    }
}
