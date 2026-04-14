using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public interface IHitResolver
{
    AttackResult Resolve(AttackData attackData);
}