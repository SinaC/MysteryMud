using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;
using TinyECS;

namespace MysteryMud.Domain.Systems;

public sealed class DeathSystem
{
    private readonly World _world;
    private readonly IFollowService _followService;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<DeathEvent> _deathEvents;

    public DeathSystem(World world, IFollowService followService, IDirtyTracker dirtyTracker, IIntentContainer intents, IEventBuffer<DeathEvent> deathEvents)
    {
        _world = world;
        _followService = followService;
        _dirtyTracker = dirtyTracker;
        _intents = intents;
        _deathEvents = deathEvents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var death in _deathEvents.GetAll())
        {
            HandleDeath(state, ref death);
        }
    }

    private void HandleDeath(GameState state, ref DeathEvent deathEvent)
    {
        var victim = deathEvent.Victim;

        if (_world.Has<Casting>(victim))
            _world.Remove<Casting>(victim);

        if (_world.Has<PlayerTag>(victim))
        {
            // forfeit claim on whoever they were fighting
            if (_world.Has<CombatState>(victim))
            {
                var target = _world.Get<CombatState>(victim).Target;
                CombatHelpers.ForfeitClaim(_world, target, victim);
            }

            _dirtyTracker.MarkDirty(victim, DirtyReason.Death);
        }

        _followService.StopFollowing(victim);
        _followService.StopAllFollowers(victim);

        // corpse first — reads CombatInitiator to determine loot owner
        CreateCorpse(victim, deathEvent.Killer);

        CombatHelpers.RemoveFromAllCombat(_world, state, victim);
        CombatHelpers.ForfeitAllClaims(_world, victim);
        CombatHelpers.RemoveFromAllThreatTable(_world, victim);
    }

    private void CreateCorpse(EntityId victim, EntityId killer)
    {
        if (!_world.Has<Location>(victim) || !_world.Has<Inventory>(victim))
            return; // can't create a corpse if we don't know where the victim is
        ref var location = ref _world.Get<Location>(victim);
        ref var inventory = ref _world.Get<Inventory>(victim);
        // create corpse
        var corpse = ItemFactory.CreateItemInRoom(_world, "corpse", $"the corpse of {EntityHelpers.DisplayName(_world, victim)}", location.Room);
        _world.Add(corpse, new Container { Capacity = 1000 });
        var containerContents = new ContainerContents { Items = [] };
        foreach (var item in inventory.Items.ToArray())
        {
            // Unequip if necessary
            ref var equipped = ref _world.TryGetRef<Equipped>(item, out var isEquipped);
            if (isEquipped)
            {
                ItemHelpers.TryUnequipItem(_world, victim, equipped.Slot, out _);
            }

            inventory.Items.Remove(item);
            ref var containedIn = ref _world.Get<ContainedIn>(item);
            containedIn.Character = EntityId.Invalid;
            containedIn.Container = corpse;
            containerContents.Items.Add(item);
        }
        _world.Add(corpse, containerContents);

        if (CombatHelpers.TryDetermineLootOwner(_world, victim, killer, out var lootOwner))
        {
            var lootOwnerGroup = _world.Has<GroupMember>(lootOwner)
                ? _world.Get<GroupMember>(lootOwner).Group
                : EntityId.Invalid;

            // autoloot for killer and group members
            ref var corpseLootIntent = ref _intents.CorpseLoot.Add();
            corpseLootIntent.Corpse = corpse;
            corpseLootIntent.LootOwner = lootOwner;
            corpseLootIntent.LootOwnerGroup = lootOwnerGroup;

            // TODO: display to killer why he/she didn't get the loot ?
            //if (lootOwner != killer && killer.Has<PlayerTag>())
            //    _msg.To(killer).Send($"{victim.DisplayName} was already engaged by {lootOwner.DisplayName}.");
        }
    }
}
