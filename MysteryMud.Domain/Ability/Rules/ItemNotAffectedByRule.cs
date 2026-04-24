using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Rules;

public class ItemNotAffectedByRule : AbilityValidationRule // opposite of AffectedByRule
{
    private readonly ulong _effectTagIndex;

    public ItemNotAffectedByRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, ItemEffectTagId effectTagId)
        : base(condition, failBehaviour, failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
    }

    public override AbilityValidationResult Validate(World world, EntityId _, EntityId target)
    {
        ref var itemEffects = ref world.Get<ItemEffects>(target);
        if ((itemEffects.Data.ActiveTags & _effectTagIndex) != 0)
            return Fail();

        return Success();
    }
}
