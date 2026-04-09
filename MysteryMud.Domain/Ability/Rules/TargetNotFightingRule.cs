using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Ability.Rules;

public class TargetNotFightingRule : IAbilityValidationRule
{
    private readonly string _failMessageKey;

    public TargetNotFightingRule(string failMessageKey)
    {
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity caster, List<Entity> targets, AbilityRuntime ability)
    {
        if (targets == null || targets.Count == 0)
            return Success();

        if (targets.Any(x => x.Has<CombatState>()))
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
            FailureMessageKey = _failMessageKey
        };
}
