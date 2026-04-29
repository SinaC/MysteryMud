using DefaultEcs;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Queries;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using System.Text;

namespace MysteryMud.Application.Commands.Commands;

public sealed class GroupCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IGroupService _groupService;

    public GroupCommand(IGameMessageService msg, IGroupService groupService)
    {
        _msg = msg;
        _groupService = groupService;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // display group members/pets/charmies if no parameter
        if (ctx.TargetCount == 0)
        {
            DisplayGroup(actor);
            return;
        }

        ref var people = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary.Kind, ctx.Primary.Index, ctx.Primary.Name, people);
        if (target == null)
        {
            _msg.To(actor).Send("They aren't here.");
            return;
        }

        if (actor == target.Value)
        {
            _msg.To(actor).Send("You can't group yourself.");
            return;
        }

        if (!target.Value.Has<PlayerTag>())
        {
            _msg.To(actor).Send("You can only group other players.");
            return;
        }

        // is target following us
        if (!target.Value.Has<Following>() || target.Value.Get<Following>().Leader != actor)
        {
            _msg.To(actor).Act("{0} is not following you.").With(target.Value);
            return;
        }

        // we are not in a group, create a group and add target
        if (!actor.Has<GroupMember>())
        {
            var group = state.World.CreateEntity();
            group.Set(new GroupInstance
            {
                Leader = actor, // already set as leader
                LootRule = LootRule.FreeForAll,
                Members = [], // use AddMember to add
                MasterLooter = default,
            });
            _groupService.AddMember(state, group, actor);
            _groupService.AddMember(state, group, target.Value);
            _msg.ToGroup(group).Act("{0} {0:b} the leader of the group.").With(actor);
            return;
        }

        // TODO: what if group is not alive

        // we are in a group
        ref var actorGroupMember = ref actor.Get<GroupMember>();
        ref var groupInstance = ref actorGroupMember.Group.Get<GroupInstance>();
        // target is in the gorup, remove target
        if (groupInstance.Members.Contains(target.Value))
        {
            _groupService.RemoveMember(state, actorGroupMember.Group, target.Value);
            return;
        }
        // target is not in the group, add target
        _groupService.AddMember(state, actorGroupMember.Group, target.Value);
    }

    private void DisplayGroup(Entity actor)
    {
        // no group nor charmies -> nothing to display
        if (!actor.Has<GroupMember>() && !actor.Has<Charmies>())
        {
            _msg.To(actor).Send("You aren't in a group.");
            return;
        }
        // if not in a group but charmies -> display charmies
        if (!actor.Has<GroupMember>())
        {
            DisplayCharmies(actor, actor);
            return;
        }
        ref var groupMember = ref actor.Get<GroupMember>();
        // in a group, display members and their charmies
        ref var groupInstance = ref groupMember.Group.Get<GroupInstance>();
        _msg.To(actor).Act($"{groupInstance.Leader.DisplayName}'s group:");
        foreach (var member in groupInstance.Members)
        {
            DisplayMember(actor, member);
            DisplayCharmies(actor, member);
        }
    }

    private void DisplayMember(Entity actor, Entity member)
    {
        var health = member.Get<Health>();
        var move = member.Get<Move>();
        var resources = BuildResources(member);
        _msg.To(actor).Send(string.Format("[{0,3} {1,3}] {2,-20} {3,5}/{4,5} hp {5} {6,5}/{7,5} mv", member.Level, /*TODO class*/"Plr", member.DisplayName.MaxLength(20), health.Current, health.Max, resources, move.Current, move.Max));
    }

    private void DisplayCharmies(Entity actor, Entity member)
    {
        if (!member.Has<Charmies>())
            return;
        ref var charmies = ref member.Get<Charmies>();
        foreach (var charmie in charmies.Entities)
        {
            var health = charmie.Get<Health>();
            var move = charmie.Get<Move>();
            var resources = BuildResources(charmie);
            _msg.To(actor).Send(string.Format("[{0,3} Pet] {1,-20} {2,5}/{3,5} hp {4} {5,5}/{6,5} mv", charmie.Level, charmie.DisplayName.MaxLength(20), health.Current, health.Max, resources, move.Current, move.Max));
        }
    }

    private static string BuildResources(Entity target)
    {
        var sb = new StringBuilder();

        AppendResource<Mana, UsesMana>(sb, target, ResourceKind.Mana, x => (x.Current, x.Max));
        AppendResource<Energy, UsesEnergy>(sb, target, ResourceKind.Energy, x => (x.Current, x.Max));
        AppendResource<Rage, UsesRage>(sb, target, ResourceKind.Rage, x => (x.Current, x.Max));

        return sb.ToString();
    }

    private static StringBuilder AppendResource<TResource, TUses>(StringBuilder sb, Entity target, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc)
       where TResource : struct
       where TUses : struct
    {
        var uses = target.Has<TUses>();
        if (!uses)
            return sb;
        if (!target.Has<TResource>())
            return sb;
        ref var resource = ref target.Get<TResource>();
        var (current, max) = getCurrentMaxFunc(resource);
        sb.AppendFormat("{0:5}/{1:5} {2}", current, max, kind.ToString().ToLowerInvariant());
        return sb;
    }
}
