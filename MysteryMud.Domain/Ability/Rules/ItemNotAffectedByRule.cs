using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class ItemNotAffectedByRule : AbilityValidationRule // opposite of AffectedByRule
{
    private readonly ulong _effectTagIndex;

    public ItemNotAffectedByRule(AbilityValidationRuleCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, ItemEffectTagId effectTagId)
        : base(condition, failBehaviour, failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
    }

    public override AbilityValidationResult Validate(Entity target)
    {
        ref var itemEffects = ref target.Get<ItemEffects>();
        if ((itemEffects.Data.ActiveTags & _effectTagIndex) != 0)
            return Fail();

        return Success();
    }
}
