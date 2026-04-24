using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Rules;

public class ItemAffectedByRule : AbilityValidationRule // opposite of NotAffectedByRule
{
    private readonly ulong _effectTagIndex;

    public ItemAffectedByRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, ItemEffectTagId effectTagId)
        : base(condition, failBehaviour, failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
    }

    public override AbilityValidationResult Validate(World world, EntityId _, EntityId target)
    {
        ref var itemEffects = ref world.Get<ItemEffects>(target);
        if ((itemEffects.Data.ActiveTags & _effectTagIndex) != _effectTagIndex)
            return Fail();

        return Success();
    }
}
