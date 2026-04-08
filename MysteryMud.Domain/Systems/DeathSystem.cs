using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class DeathSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<DeathEvent> _deathEvents;

    public DeathSystem(IGameMessageService msg, IIntentContainer intents, IEventBuffer<DeathEvent> deathEvents)
    {
        _msg = msg;
        _intents = intents;
        _deathEvents = deathEvents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var death in _deathEvents.GetAll())
        {
            HandleDeath(state.World, ref death);
        }
    }

    private void HandleDeath(World world, ref DeathEvent deathEvent)
    {
        deathEvent.Victim.Remove<Casting>();
        RemoveFromCombat(world, deathEvent.Victim);
        CreateCorpse(world, deathEvent.Victim, deathEvent.Killer);

        // TODO
        //// Queue XP reward
        //ctx.QueueIntent(new GrantRewardIntent
        //{
        //    Target = death.Killer,
        //    RewardSpec = new RewardSpec { XP = death.ExperienceValue }
        //});
        // TODO: RewardSystem
        //-Applies coins, XP, items
        //- Pushes messages to players
    }

    // TODO: this could be optimized by having a "Targeting" component that lists all entities targeting a given entity, so we don't have to scan everyone in the world for combat state every time someone dies. We would need to maintain this list as combat states are added/removed, but it would make removing combat state on death much more efficient.
    // mutually remove combat state from victim and anyone targeting the victim in one query if possible
    private static void RemoveFromCombat(World world, Entity victim)
    {
        // remove from combat
        victim.Remove<CombatState>();
        // remove combat state for anyone targeting this entity
        var query = new QueryDescription()
          .WithAll<CombatState>();
        world.Query(query, (Entity actor, ref CombatState combat) =>
        {
            if (combat.Target == victim)
                actor.Remove<CombatState>();
        });
    }

    private void CreateCorpse(World world, Entity victim, Entity killer)
    {
        if (!victim.Has<Location, Inventory>())
            return; // can't create a corpse if we don't know where the victim is
        ref var location = ref victim.Get<Location>();
        ref var inventory = ref victim.Get<Inventory>();
        // create corpse
        var corpse = ItemFactory.CreateItemInRoom(world, "corpse", $"the corpse of {victim.DisplayName}", location.Room);
        corpse.Add(new Container { Capacity = 1000 });
        var containerContents = new ContainerContents { Items = [] };
        foreach (var item in inventory.Items.ToArray())
        {
            // Unequip if necessary
            ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
            if (isEquipped)
            {
                ItemHelpers.TryUnequipItem(victim, equipped.Slot, out _);
            }

            inventory.Items.Remove(item);
            ref var containedIn = ref item.Get<ContainedIn>();
            containedIn.Character = Entity.Null;
            containedIn.Container = corpse;
            containerContents.Items.Add(item);
        }
        corpse.Add(containerContents);

        // trigger autoloot (TODO: only for player and if autoloot enabled)
        ref var lootIntent = ref _intents.Loot.Add();
        lootIntent.Looter = killer;
        lootIntent.Corpse = corpse;
    }
}
