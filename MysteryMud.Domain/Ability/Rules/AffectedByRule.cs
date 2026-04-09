using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class AffectedByRule : IAbilityValidationRule // opposite of NotAffectedByRule
{
    private readonly ulong _effectTagIndex;
    private readonly string _failMessageKey;

    public AffectedByRule(EffectTagId effectTagId, string failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity caster, List<Entity> targets, AbilityRuntime ability)
    {
        ref var characterEffects = ref caster.Get<CharacterEffects>();
        if ((characterEffects.ActiveTags & _effectTagIndex) != _effectTagIndex)
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
