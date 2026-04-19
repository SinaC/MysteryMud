using Arch.Core;
using MysteryMud.Core;

namespace MysteryMud.Domain.Services;

public interface IGroupService
{
    void AddMember(GameState state, Entity group, Entity member);
    void RemoveMember(GameState state, Entity group, Entity member);
}