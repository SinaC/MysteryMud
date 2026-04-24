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
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class CleanupSystem
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IFollowService _followService;
    private readonly IGroupService _groupService;
    private readonly IEffectLifecycleManager _effectLifecycleManager;

    public CleanupSystem(World world, ILogger logger, IFollowService followService, IGroupService groupService, IEffectLifecycleManager effectLifecycleManager)
    {
        _world = world;
        _logger = logger;
        _followService = followService;
        _groupService = groupService;
        _effectLifecycleManager = effectLifecycleManager;
    }

    public void Tick(GameState state)
    {
        CleanGroups();
        CleanExpiredEffects();
        CleanDisconnectedPlayers(state);
        CleanNpcs(state);
        CleanItems(state);
    }

    private static readonly QueryDescription _disbandedQueryDesc = new QueryDescription()
        .WithAll<DisbandedTag>();
    private void CleanGroups()
    {
        // destroy disbanded groups
        var toDestroy = new List<EntityId>();
        _world.Query(_disbandedQueryDesc, (EntityId group,
            ref DisbandedTag _) =>
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up disbanded group");

            toDestroy.Add(group);
        });
        //
        foreach (var entity in toDestroy)
        {
            if (_world.IsAlive(entity))
                _world.DestroyEntity(entity);
        }
    }

    private static readonly QueryDescription _expiredEffectsQueryDesc = new QueryDescription()
        .WithAll<ExpiredTag>();

    private void CleanExpiredEffects()
    {
        // destroy expired effects
        var toDestroy = new List<EntityId>();
        _world.Query(_expiredEffectsQueryDesc, (EntityId effect,
            ref ExpiredTag _) =>
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up expired effect {effectName}", EntityHelpers.DebugName(_world, effect));

            // remove the effect from the target's CharacterEffects
            _effectLifecycleManager.RemoveEffect(effect);

            toDestroy.Add(effect);
        });
        //
        foreach (var entity in toDestroy)
        {
            if (_world.IsAlive(entity))
                _world.DestroyEntity(entity);
        }
    }

    private static readonly QueryDescription _disconnectedPlayersQueryDesc = new QueryDescription()
        .WithAll<DisconnectedTag>();

    private void CleanDisconnectedPlayers(GameState state)
    {
        // destroy disconnected players
        var toDestroy = new List<EntityId>();
        _world.Query(_disconnectedPlayersQueryDesc, (EntityId player,
            ref DisconnectedTag _) =>
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up disconnected player {characterName}", EntityHelpers.DebugName(_world, player));

            _followService.StopFollowing(player);
            _followService.StopAllFollowers(player);
            ref var groupMember = ref _world.TryGetRef<GroupMember>(player, out var inGroup);
            if (inGroup)
                _groupService.RemoveMember(groupMember.Group, player);
            CombatHelpers.RemoveFromAllCombat(_world, state, player);
            CombatHelpers.RemoveFromAllThreatTable(_world, player);
            CombatHelpers.ForfeitAllClaims(_world, player);
            RemoveCharacterFromRoomContents(player);
            CollectCharacterEffects(player, toDestroy);

            // TODO: destroy any items the character is carrying or equipped with
            // TODO: if the character is a pet, remove it from its owner's pet list

            // TODO: call PersistenceSystem.SaveOnDisconnectAsync

            toDestroy.Add(player);
        });
        //
        foreach (var entity in toDestroy)
        {
            if (_world.IsAlive(entity))
                _world.DestroyEntity(entity);
        }
    }

    private static readonly QueryDescription _destroyNpcsQuery = new QueryDescription()
        .WithAll<Dead, NpcTag>();

    private void CleanNpcs(GameState state)
    {
        // destroy NPCs
        var toDestroy = new List<EntityId>();
        _world.Query(_destroyNpcsQuery, (EntityId npc,
            ref Dead _,
            ref NpcTag _) =>
        {
            ref var location = ref _world.TryGetRef<Location>(npc, out var hasLocation);

            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up npc {characterName} from room {roomName}", EntityHelpers.DebugName(_world, npc), hasLocation ? EntityHelpers.DebugName(_world, location.Room) : "???");

            _followService.StopFollowing(npc);
            CombatHelpers.RemoveFromAllCombat(_world, state, npc);
            CombatHelpers.RemoveFromAllThreatTable(_world, npc);
            RemoveCharacterFromRoomContents(npc);
            CollectCharacterEffects(npc, toDestroy);

            // TODO: destroy any items the character is carrying or equipped with
            // TODO: if the character is a pet, remove it from its owner's pet list

            toDestroy.Add(npc);
        });
        //
        foreach (var entity in toDestroy)
        {
            if (_world.IsAlive(entity))
                _world.DestroyEntity(entity);
        }
    }

    private static readonly QueryDescription _destroyItemsQuery = new QueryDescription()
        .WithAll<DestroyedTag>()
        .WithAny<Location, ContainedIn, Equipped>();

    private void CleanItems(GameState state)
    {
        // destroy items
        var toDestroy = new List<EntityId>();
        _world.Query(_destroyItemsQuery, (EntityId item, ref DestroyedTag destroyedTag) =>
        {
            // check if the item is on the ground
            ref var location = ref _world.TryGetRef<Location>(item, out var hasLocation);
            if (hasLocation)
            {
                _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from location {locationName}", EntityHelpers.DebugName(_world, item), EntityHelpers.DebugName(_world, location.Room));

                ref var roomContents = ref _world.Get<RoomContents>(location.Room);
                roomContents.Items.Remove(item);
            }
            // check if the item is in a container or inventory
            ref var containedIn = ref _world.TryGetRef<ContainedIn>(item, out var hasContainedIn);
            if (hasContainedIn)
            {
                if (containedIn.Character != EntityId.Invalid)
                {
                    _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from inventory of {inventoryOwnerName}", EntityHelpers.DebugName(_world, item), EntityHelpers.DebugName(_world, containedIn.Character));

                    ref var inventory = ref _world.Get<Inventory>(containedIn.Character);
                    inventory.Items.Remove(item);
                }
                else if (containedIn.Container != EntityId.Invalid)
                {
                    _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from container {containerName}", EntityHelpers.DebugName(_world, item), EntityHelpers.DebugName(_world, containedIn.Container));

                    ref var containerContents = ref _world.Get<ContainerContents>(containedIn.Container);
                    containerContents.Items.Remove(item);
                }
            }
            // check if the item is equipped should never happen)
            ref var equipped = ref _world.TryGetRef<Equipped>(item, out var isEquipped);
            if (isEquipped)
            {
                ref var equipment = ref _world.Get<Equipment>(equipped.Wearer);
                foreach (var slot in equipment.Slots.Keys.ToList())
                {
                    if (equipment.Slots[slot] == item)
                    {
                        _logger.LogInformation(LogEvents.Cleanup, "Cleaning up item {itemName} from equipment of {wearerName} in slot {slot}", EntityHelpers.DebugName(_world, item), EntityHelpers.DebugName(_world, equipped.Wearer), slot);

                        equipment.Slots[slot] = EntityId.Invalid;
                    }
                }
            }
            // TODO: if container: destroy content
            //
            CollectItemEffects(item, toDestroy);

            toDestroy.Add(item);
        });
        //
        foreach (var entity in toDestroy)
        {
            if (_world.IsAlive(entity))
                _world.DestroyEntity(entity);
        }
    }

    private void RemoveCharacterFromRoomContents(EntityId victim)
    {
        ref var location = ref _world.TryGetRef<Location>(victim, out var hasLocation);
        if (!hasLocation)
            return; // can't remove from room contents if we don't know where the victim is
        ref var roomContents = ref _world.Get<RoomContents>(location.Room);
        roomContents.Characters.Remove(victim);
    }

    private void CollectCharacterEffects(EntityId player, List<EntityId> toDestroy)
    {
        ref var characterEffects = ref _world.Get<CharacterEffects>(player);
        foreach (var effect in characterEffects.Data.Effects)
        {
            if (_world.IsAlive(effect))
                toDestroy.Add(effect);
        }
    }

    private void CollectItemEffects(EntityId player, List<EntityId> toDestroy)
    {
        ref var characterEffects = ref _world.Get<ItemEffects>(player);
        foreach (var effect in characterEffects.Data.Effects)
        {
            if (_world.IsAlive(effect))
                toDestroy.Add(effect);
        }
    }
}