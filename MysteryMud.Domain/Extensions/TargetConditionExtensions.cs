using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Extensions;

public static class TargetConditionExtensions
{
    public static bool Matches(this TargetCondition condition, World world, EntityId entity) => condition switch
    {
        TargetCondition.IsCharacter => world.Has<CharacterTag>(entity),
        TargetCondition.IsItem => world.Has<ItemTag>(entity),
        TargetCondition.IsNPC => world.Has<NpcTag>(entity),
        TargetCondition.IsPlayer => world.Has<PlayerTag>(entity),
        TargetCondition.IsWeapon => world.Has<Weapon>(entity),
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
