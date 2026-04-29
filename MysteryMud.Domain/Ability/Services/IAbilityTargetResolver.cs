using DefaultEcs;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Services;

public interface IAbilityTargetResolver
{
    TargetResolutionResult Resolve(
            in Entity source,
            TargetKind targetKind,
            int targetIndex,
            string targetName,
            AbilityTargetingDefinition targeting);

    TargetResolutionResult Resolve(
            in Entity source,
            AbilityTargetingDefinition targeting);
}
