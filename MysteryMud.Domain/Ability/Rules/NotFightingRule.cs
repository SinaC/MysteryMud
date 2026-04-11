using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class NotFightingRule : IAbilityValidationRule
{
    private readonly AbilityValidationFailBehaviour _failBehaviour;
    private readonly string _failMessageKey;

    public NotFightingRule(AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
    {
        _failBehaviour = failBehaviour;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity target)
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
            FailBehaviour = _failBehaviour,
            FailMessageKey = _failMessageKey
        };
}
