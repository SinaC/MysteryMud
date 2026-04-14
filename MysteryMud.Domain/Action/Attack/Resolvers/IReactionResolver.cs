using MysteryMud.Core.Intent;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public interface IReactionResolver
{
    void Resolve(IIntentContainer intentContainer, AttackResult result);
}