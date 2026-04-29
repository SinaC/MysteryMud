using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class DeathSystem
{
    private readonly IFollowService _followService;
    private readonly ICombatService _combatService;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<DeathEvent> _deathEvents;

    public DeathSystem(IFollowService followService, ICombatService combatService, IDirtyTracker dirtyTracker, IIntentContainer intents, IEventBuffer<DeathEvent> deathEvents)
    {
        _followService = followService;
        _combatService = combatService;
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

        if (victim.Has<Casting>())
            victim.Remove<Casting>();

        if (victim.Has<PlayerTag>())
        {
            // forfeit claim on whoever they were fighting
            if (victim.Has<CombatState>())
            {
                var target = victim.Get<CombatState>().Target;
                _combatService.ForfeitClaim(target, victim);
            }

            _dirtyTracker.MarkDirty(victim, DirtyReason.Death);
        }

        _followService.StopFollowing(victim);
        _followService.StopAllFollowers(victim);

        // corpse first — reads CombatInitiator to determine loot owner
        CreateCorpse(state.World, victim, deathEvent.Killer);

        _combatService.RemoveFromAllCombat(state, victim);
        _combatService.ForfeitAllClaims(state.World, victim);
        _combatService.RemoveFromAllThreatTable(state.World, victim);
    }

    private void CreateCorpse(World world, Entity victim, Entity killer)
    {
        if (!victim.Has<Location>() || !victim.Has<Inventory>())
            return; // can't create a corpse if we don't know where the victim is
        ref var location = ref victim.Get<Location>();
        ref var inventory = ref victim.Get<Inventory>();
        // create corpse
        var corpse = ItemFactory.CreateItemInRoom(world, "corpse", $"the corpse of {victim.DisplayName}", location.Room);
        corpse.Set(new Container { Capacity = 1000 });
        var containerContents = new ContainerContents { Items = [] };
        foreach (var item in inventory.Items.ToArray())
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                ItemHelpers.TryUnequipItem(victim, equipped.Slot, out _);
            }

            inventory.Items.Remove(item);
            ref var containedIn = ref item.Get<ContainedIn>();
            containedIn.Character = default;
            containedIn.Container = corpse;
            containerContents.Items.Add(item);
        }
        corpse.Set(containerContents);

        if (_combatService.TryDetermineLootOwner(victim, killer, out var lootOwner))
        {
            var lootOwnerGroup = lootOwner.Has<GroupMember>()
                ? lootOwner.Get<GroupMember>().Group
                : default;

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
