using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Services;

public interface IAbilityTargetResolver
{
    TargetResolutionResult Resolve(
            in EntityId source,
            TargetKind targetKind,
            int targetIndex,
            string targetName,
            AbilityTargetingDefinition targeting);

    TargetResolutionResult Resolve(
            in EntityId source,
            AbilityTargetingDefinition targeting);
}
