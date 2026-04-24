using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class LeaveCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IGroupService _groupService;

    public LeaveCommand(World world, IGameMessageService msg, IGroupService groupService)
    {
        _world = world;
        _msg = msg;
        _groupService = groupService;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var groupMember = ref _world.TryGetRef<GroupMember>(actor, out var isGrouped);
        // we are not in a group, create a group and add target
        if (!isGrouped)
        {
            _msg.To(actor).Send("You aren't in a group.");
            return;
        }

        _groupService.RemoveMember(groupMember.Group, actor);
    }
}