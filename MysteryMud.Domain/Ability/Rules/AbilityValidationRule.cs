using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public abstract class AbilityValidationRule : IAbilityValidationRule
{
    private readonly AbilityValidationRuleCondition _condition;
    private readonly AbilityValidationFailBehaviour _failBehaviour;
    private readonly string _failMessageKey;

    protected AbilityValidationRule(AbilityValidationRuleCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
    {
        _condition = condition;
        _failBehaviour = failBehaviour;
        _failMessageKey = failMessageKey;
    }

    public bool IsCandidateForValidation(Entity target)
    {
        switch (_condition)
        {
            case AbilityValidationRuleCondition.IsCharacter: return target.Has<CharacterTag>();
            case AbilityValidationRuleCondition.IsItem: return target.Has<ItemTag>();
            case AbilityValidationRuleCondition.IsNPC: return target.Has<NpcTag>();
            case AbilityValidationRuleCondition.IsPlayer: return target.Has<PlayerTag>();
            case AbilityValidationRuleCondition.IsWeapon: return target.Has<Weapon>();
        }
        return true;
    }

    public abstract AbilityValidationResult Validate(Entity target);

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
