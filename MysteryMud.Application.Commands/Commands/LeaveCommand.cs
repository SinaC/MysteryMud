using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class LeaveCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IGroupService _groupService;

    public LeaveCommand(IGameMessageService msg, IGroupService groupService)
    {
        _msg = msg;
        _groupService = groupService;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        // we are not in a group, create a group and add target
        if (!actor.Has<GroupMember>())
        {
            _msg.To(actor).Send("You aren't in a group.");
            return;
        }

        ref var groupMember = ref actor.Get<GroupMember>();
        _groupService.RemoveMember(state, groupMember.Group, actor);
    }
}