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
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Queries;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using System.Text;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Application.Commands.Commands;

public sealed class GroupCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IGroupService _groupService;

    public GroupCommand(World world, IGameMessageService msg, IGroupService groupService)
    {
        _world = world;
        _msg = msg;
        _groupService = groupService;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // display group members/pets/charmies if no parameter
        if (ctx.TargetCount == 0)
        {
            DisplayGroup(actor);
            return;
        }

        ref var room = ref _world.Get<Location>(actor).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        var target = EntityFinder.SelectSingleTarget(_world, actor, ctx.Primary.Kind, ctx.Primary.Index, ctx.Primary.Name, people);
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

        if (!_world.Has<PlayerTag>(target.Value))
        {
            _msg.To(actor).Send("You can only group other players.");
            return;
        }

        // is target following us
        ref var targetFollowing = ref _world.TryGetRef<Following>(target.Value, out var isTargetFollowing);
        if (!isTargetFollowing || targetFollowing.Leader != actor)
        {
            _msg.To(actor).Act("{0} is not following you.").With(target.Value);
            return;
        }

        ref var actorGroupMember = ref _world.TryGetRef<GroupMember>(actor, out var isActorGrouped);
        // we are not in a group, create a group and add target
        if (!isActorGrouped)
        {
            var group = _world.CreateEntity(new GroupInstance
            {
                Leader = actor, // already set as leader
                LootRule = LootRule.FreeForAll,
                Members = [], // use AddMember to add
                MasterLooter = EntityId.Invalid,
            });
            _groupService.AddMember(state, group, actor);
            _groupService.AddMember(state, group, target.Value);
            _msg.ToGroup(group).Act("{0} {0:b} the leader of the group.").With(actor);
            return;
        }
        // we are in a group
        ref var groupInstance = ref _world.Get<GroupInstance>(actorGroupMember.Group);
        // target is in the gorup, remove target
        if (groupInstance.Members.Contains(target.Value))
        {
            _groupService.RemoveMember(actorGroupMember.Group, target.Value);
            return;
        }
        // target is not in the group, add target
        _groupService.AddMember(state, actorGroupMember.Group, target.Value);
    }

    private void DisplayGroup(EntityId actor)
    {
        ref var groupMember = ref _world.TryGetRef<GroupMember>(actor, out var isInGroup); 
        ref var charmies = ref _world.TryGetRef<Charmies>(actor, out var hasCharmies);
        // no group nor charmies -> nothing to display
        if (!isInGroup && !hasCharmies)
        {
            _msg.To(actor).Send("You aren't in a group.");
            return;
        }
        // if not in a group but charmies -> display charmies
        if (!isInGroup)
        {
            DisplayCharmies(actor, actor);
            return;
        }
        // in a group, display members and their charmies
        ref var groupInstance = ref _world.Get<GroupInstance>(groupMember.Group);
        _msg.To(actor).Send($"{EntityHelpers.DisplayName(_world, groupInstance.Leader)}'s group:");
        foreach (var member in groupInstance.Members)
        {
            DisplayMember(actor, member);
            DisplayCharmies(actor, member);
        }
    }

    private void DisplayMember(EntityId actor, EntityId member)
    {
        var level = _world.Get<Level>(member);
        var health = _world.Get<Health>(member);
        var move = _world.Get<Move>(member);
        var resources = BuildResources(member);
        _msg.To(actor).Send(string.Format("[{0,3} {1,3}] {2,-20} {3,5}/{4,5} hp {5} {6,5}/{7,5} mv", level.Value, /*TODO class*/"Plr", EntityHelpers.DisplayName(_world, member).MaxLength(20), health.Current, health.Max, resources, move.Current, move.Max));
    }

    // TODO: pets

    private void DisplayCharmies(EntityId actor, EntityId member)
    {
        ref var charmies = ref _world.TryGetRef<Charmies>(member, out var hasCharmies);
        if (!hasCharmies)
            return;
        foreach (var charmie in charmies.Entities)
        {
            var level = _world.Get<Level>(charmie);
            var health = _world.Get<Health>(charmie);
            var move = _world.Get<Move>(charmie);
            var resources = BuildResources(charmie);
            _msg.To(actor).Send(string.Format("[{0,3} Pet] {1,-20} {2,5}/{3,5} hp {4} {5,5}/{6,5} mv", level.Value, EntityHelpers.DisplayName(_world, charmie).MaxLength(20), health.Current, health.Max, resources, move.Current, move.Max));
        }
    }

    private string BuildResources(EntityId target)
    {
        var sb = new StringBuilder();

        AppendResource<Mana, UsesMana>(sb, target, ResourceKind.Mana, x => (x.Current, x.Max));
        AppendResource<Energy, UsesEnergy>(sb, target, ResourceKind.Energy, x => (x.Current, x.Max));
        AppendResource<Rage, UsesRage>(sb, target, ResourceKind.Rage, x => (x.Current, x.Max));

        return sb.ToString();
    }

    private StringBuilder AppendResource<TResource, TUses>(StringBuilder sb, EntityId target, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc)
       where TResource : struct
       where TUses : struct
    {
        var uses = _world.Has<TUses>(target);
        if (!uses)
            return sb;
        ref var resource = ref _world.TryGetRef<TResource>(target, out var hasResource);
        if (!hasResource)
            return sb;
        var (current, max) = getCurrentMaxFunc(resource);
        sb.AppendFormat("{0:5}/{1:5} {2}", current, max, kind.ToString().ToLowerInvariant());
        return sb;
    }
}
