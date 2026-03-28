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
    private IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<DeathEvent> _deathEvents;

    public DeathSystem(IGameMessageService msg, IIntentContainer intents, IEventBuffer<DeathEvent> deathEvents)
    {
        _msg = msg;
        _intents = intents;
        _deathEvents = deathEvents;
    }

    public void Tick(GameState gameState)
    {
        foreach (ref var death in _deathEvents.GetAll())
        {
            HandleDeath(gameState.World, ref death);
        }
    }

    private void HandleDeath(World world, ref DeathEvent deathEvent)
    {
        _msg.To(deathEvent.Dead).Send("%RYou have been KILLED%x");
        _msg.ToRoom(deathEvent.Dead).Act("{0} is dead").With(deathEvent.Dead);

        CreateCorpse(world, deathEvent.Dead, deathEvent.Killer);
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

        // trigger autoloot
        ref var lootIntent = ref _intents.Loot.Add();
        lootIntent.Looter = killer;
        lootIntent.Corpse = corpse;
    }
}
