using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class NotFightingRule : AbilityValidationRule
{
    public NotFightingRule(AbilityValidationRuleCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
        : base(condition, failBehaviour, failMessageKey)
    {
    }

    public override AbilityValidationResult Validate(Entity target)
    {
        if (target.Has<CombatState>())
            return Fail();

        return Success();
    }
}
