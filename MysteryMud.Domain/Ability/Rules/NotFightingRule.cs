using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Rules;

public class NotFightingRule : AbilityValidationRule
{
    public NotFightingRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
        : base(condition, failBehaviour, failMessageKey)
    {
    }

    public override AbilityValidationResult Validate(World world, EntityId _, EntityId target)
    {
        if (world.Has<CombatState>(target))
            return Fail();

        return Success();
    }
}
