using MysteryMud.Core.Contracts;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public interface IReactionResolver
{
    void Resolve(IIntentContainer intentContainer, AttackResult result);
}