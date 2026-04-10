using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityRuntime
{
    public int Id; // generated
    public string Name = default!;
    public AbilityKind Kind;
    public int CastTime = 0; // 0 means instant cast
    public int Cooldown = 0;
    public List<ResourceCost> Costs = [];
    public AbilityTargeting Targeting;
    public int ExecutorId;
    public List<int> EffectIds = [];
    public List<int> FailureEffectIds = [];
    public Dictionary<string, string> Messages = []; // TODO: actor/room messages ?
    public List<IAbilityValidationRule> ValidationRules = [];
}
