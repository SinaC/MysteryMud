using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class NotFightingRule : IAbilityValidationRule
{
    private readonly AbilityValidationRuleFailActions _failActions;
    private readonly string _failMessageKey;

    public NotFightingRule(AbilityValidationRuleFailActions failActions, string failMessageKey)
    {
        _failActions = failActions;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity target, AbilityRuntime ability)
    {
        if (target.Has<CombatState>())
            return Fail();

        return Success();
    }

    private AbilityValidationResult Success()
        => new()
        {
            Success = true
        };

    private AbilityValidationResult Fail()
        => new()
        {
            Success = false,
            FailActions = _failActions,
            FailureMessageKey = _failMessageKey
        };
}
