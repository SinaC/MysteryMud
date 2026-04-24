using TinyECS;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public abstract class AbilityValidationRule : IAbilityValidationRule
{
    private readonly TargetCondition _condition;
    private readonly AbilityValidationFailBehaviour _failBehaviour;
    private readonly string _failMessageKey;

    protected AbilityValidationRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
    {
        _condition = condition;
        _failBehaviour = failBehaviour;
        _failMessageKey = failMessageKey;
    }

    public bool IsCandidateForValidation(World world, EntityId target)
        => _condition.Matches(world, target);

    public abstract AbilityValidationResult Validate(World world, EntityId source, EntityId target);

    protected AbilityValidationResult Success()
        => new() { Success = true };

    protected AbilityValidationResult Fail()
        => new()
        {
            Success = false,
            FailBehaviour = _failBehaviour,
            FailMessageKey = _failMessageKey
        };
}
