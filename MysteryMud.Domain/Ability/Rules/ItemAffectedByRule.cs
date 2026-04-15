using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class ItemAffectedByRule : IAbilityValidationRule // opposite of NotAffectedByRule
{
    private readonly ulong _effectTagIndex;
    private readonly AbilityValidationFailBehaviour _failBehaviour;
    private readonly string _failMessageKey;

    public ItemAffectedByRule(ItemEffectTagId effectTagId, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
        _failBehaviour = failBehaviour;
        _failMessageKey = failMessageKey;
    }

    public bool CanBeValidated(Entity target)
        => target.Has<ItemEffects>();

    public AbilityValidationResult Validate(Entity target)
    {
        ref var itemEffects = ref target.Get<ItemEffects>();
        if ((itemEffects.Data.ActiveTags & _effectTagIndex) != _effectTagIndex)
            return new()
            {
                Success = false,
                FailBehaviour = _failBehaviour,
                FailMessageKey = _failMessageKey
            };

        return new()
        {
            Success = true
        };
    }
}
