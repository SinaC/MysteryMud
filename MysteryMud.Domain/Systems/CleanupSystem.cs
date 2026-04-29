using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;

namespace MysteryMud.Domain.Systems;

public class CleanupSystem
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IFollowService _followService;
    private readonly IGroupService _groupService;
    private readonly ICombatService _combatService;
    private readonly IEffectLifecycleManager _effectLifecycleManager;
    private readonly EntitySet _disbandedGroupEntitySet;
    private readonly EntitySet _expiredEffectsSet;
    private readonly EntitySet _disconnectedPlayersSet;
    private readonly EntitySet _deadNpcsSet;
    private readonly EntitySet _destroyedItemsSet;

    public CleanupSystem(World world, ILogger logger, IFollowService followService, IGroupService groupService, ICombatService combatService, IEffectLifecycleManager effectLifecycleManager)
    {
        _world = world;
        _logger = logger;
        _followService = followService;
        _groupService = groupService;
        _combatService = combatService;
        _effectLifecycleManager = effectLifecycleManager;

        _disbandedGroupEntitySet = world
            .GetEntities()
            .With<DisbandedTag>()
            .AsSet();
        _expiredEffectsSet = world
            .GetEntities()
            .With<ExpiredTag>()
            .AsSet();
        _disconnectedPlayersSet = world
            .GetEntities()
            .With<DisconnectedTag>()
            .AsSet();
        _deadNpcsSet = world
            .GetEntities()
            .With<DeadTag>()
            .With<NpcTag>()
            .AsSet();
        _destroyedItemsSet = world
            .GetEntities()
            .With<DestroyedTag>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        CleanDisbandedGroups();
        CleanExpiredEffects();
        CleanDisconnectedPlayers(state);
        CleanDeadNpcs(state);
        CleanDestroyedItems(state);
    }

    private void CleanDisbandedGroups()
    {
        var toDestroy = new List<Entity>();
        foreach (var group in _disbandedGroupEntitySet.GetEntities())
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up disbanded group {groupName}", group.DebugName);

            toDestroy.Add(group);
        }

        foreach(var toDestroyGroup in toDestroy.Where(x => x.IsAlive))
        {
            toDestroyGroup.Dispose();
        }
    }

    private void CleanExpiredEffects()
    {
        var toDestroy = new List<Entity>();
        foreach (var effect in _expiredEffectsSet.GetEntities())
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up expired effect {effectName}", effect.DebugName);

            // remove the effect from the target's CharacterEffects
            _effectLifecycleManager.RemoveEffect(effect);

            toDestroy.Add(effect);
        }
        foreach (var toDestroyGroup in toDestroy.Where(x => x.IsAlive))
        {
            toDestroyGroup.Dispose();
        }
    }

    private void CleanDisconnectedPlayers(GameState state)
    {
        var toDestroy = new List<Entity>();
        foreach (var player in _disconnectedPlayersSet.GetEntities())
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up disconnected player {characterName}", player.DebugName);

            _followService.StopFollowing(player);
            _followService.StopAllFollowers(player);
            if (player.Has<GroupMember>())
            {
                ref var groupMember = ref player.Get<GroupMember>();
                _groupService.RemoveMember(state, groupMember.Group, player);
            }
            _combatService.RemoveFromAllCombat(state, player);
            _combatService.RemoveFromAllThreatTable(_world, player);
            _combatService.ForfeitAllClaims(_world, player);
            RemoveFromRoomContents(player);
            CollectCharacterEffects(player, toDestroy);

            toDestroy.Add(player);
        }

        foreach (var toDestroyGroup in toDestroy.Where(x => x.IsAlive))
        {
            toDestroyGroup.Dispose();
        }
    }

    private void CleanDeadNpcs(GameState state)
    {
        var toDestroy = new List<Entity>();
        foreach(var npc in _deadNpcsSet.GetEntities())
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up npc {characterName}", npc.DebugName);

            _followService.StopFollowing(npc);
            _followService.StopAllFollowers(npc);
            _combatService.RemoveFromAllCombat(state, npc);
            _combatService.RemoveFromAllThreatTable(_world, npc);
            RemoveFromRoomContents(npc);
            CollectCharacterEffects(npc, toDestroy);

            toDestroy.Add(npc);
        }
        foreach (var toDestroyGroup in toDestroy.Where(x => x.IsAlive))
        {
            toDestroyGroup.Dispose();
        }
    }

    private void CleanDestroyedItems(GameState state)
    {
        var toDestroy = new List<Entity>();
        foreach (var item in _destroyedItemsSet.GetEntities())
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up destroyed item {itemName}", item.DebugName);

            // check if the item is on the ground
            if (item.Has<Location>())
            {
                ref var location = ref item.Get<Location>();

                _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from location {locationName}", item.DebugName, location.Room.DebugName);

                ref var roomContents = ref location.Room.Get<RoomContents>();
                roomContents.Items.Remove(item);
            }
            // check if the item is in a container or inventory
            if (item.Has<ContainedIn>())
            {
                ref var containedIn = ref item.Get<ContainedIn>();
                if (containedIn.Character != default)
                {
                    _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from inventory of {inventoryOwnerName}", item.DebugName, containedIn.Character.DebugName);

                    ref var inventory = ref containedIn.Character.Get<Inventory>();
                    inventory.Items.Remove(item);
                }
                else if (containedIn.Container != default)
                {
                    _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from container {containerName}", item.DebugName, containedIn.Container.DebugName);

                    ref var containerContents = ref containedIn.Container.Get<ContainerContents>();
                    containerContents.Items.Remove(item);
                }
            }
            // check if the item is equipped should never happen)
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                ref var equipment = ref equipped.Wearer.Get<Equipment>();
                foreach (var slot in equipment.Slots.Keys.ToList())
                {
                    if (equipment.Slots[slot] == item)
                    {
                        _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from equipment of {wearerName} in slot {slot}", item.DebugName, equipped.Wearer.DebugName, slot);

                        equipment.Slots[slot] = default;
                    }
                }
            }

            // TODO: if container: destroy content
            //
            CollectItemEffects(item, toDestroy);

            toDestroy.Add(item);
        }
        foreach (var toDestroyGroup in toDestroy.Where(x => x.IsAlive))
        {
            toDestroyGroup.Dispose();
        }
    }


    private static void RemoveFromRoomContents(Entity victim)
    {
        if (!victim.Has<Location>())
            return;
        ref var location = ref victim.Get<Location>();
        ref var roomContents = ref location.Room.Get<RoomContents>();
        roomContents.Characters.Remove(victim);
    }

    private static void CollectCharacterEffects(Entity player, List<Entity> toDestroy)
    {
        ref var characterEffects = ref player.Get<CharacterEffects>();
        foreach (var effect in characterEffects.Data.Effects)
        {
            if (effect.IsAlive)
                toDestroy.Add(effect);
        }
    }

    private static void CollectItemEffects(Entity item, List<Entity> toDestroy)
    {
        ref var itemEffects = ref item.Get<ItemEffects>();
        foreach (var effect in itemEffects.Data.Effects)
        {
            if (effect.IsAlive)
                toDestroy.Add(effect);
        }
    }
}