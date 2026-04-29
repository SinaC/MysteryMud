using DefaultEcs;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class TargetConditionExtensions
{
    public static bool Matches(this TargetCondition condition, Entity entity) => condition switch
    {
        TargetCondition.IsCharacter => entity.Has<CharacterTag>(),
        TargetCondition.IsItem => entity.Has<ItemTag>(),
        TargetCondition.IsNPC => entity.Has<NpcTag>(),
        TargetCondition.IsPlayer => entity.Has<PlayerTag>(),
        TargetCondition.IsWeapon => entity.Has<Weapon>(),
        // TODO: IsArmor
        _ => true,
    };

    public static int Specificity(this TargetCondition condition) => condition switch
    {
        TargetCondition.IsPlayer => 2,
        TargetCondition.IsNPC => 2,
        TargetCondition.IsWeapon => 2,
        TargetCondition.IsArmor => 2,
        TargetCondition.IsItem => 1,
        TargetCondition.IsCharacter => 1,
        _ => 0
    };
}
