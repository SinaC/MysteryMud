using TinyECS;
using MysteryMud.Core;

namespace MysteryMud.Domain.Services;

public interface IGroupService
{
    void AddMember(GameState state, EntityId group, EntityId member);
    void RemoveMember(EntityId group, EntityId member);
    void Disband(EntityId group);
}