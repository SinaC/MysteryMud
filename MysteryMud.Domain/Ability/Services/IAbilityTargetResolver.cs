using DefaultEcs;
using MysteryMud.Core;
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
            AbilityTargetingDefinition targeting,
            GameState state);

    TargetResolutionResult Resolve(
            in Entity source,
            AbilityTargetingDefinition targeting,
            GameState state);
}
