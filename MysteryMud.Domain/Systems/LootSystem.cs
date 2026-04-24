using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;
using TinyECS;

namespace MysteryMud.Domain.Systems;

public sealed class LootSystem
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<ItemLootedEvent> _itemLootedEvents;

    public LootSystem(World world, IGameMessageService msg, IIntentContainer intents, IEventBuffer<ItemLootedEvent> itemLootedEvents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
        _itemLootedEvents = itemLootedEvents;
    }

    // Quest items — each eligible player gets their own copy, regardless of group loot rules
    // Priority loot — killer(or master looter) gets first pick with autoloot
    // Group loot — remaining items distributed to group members with autoloot
    // Autosac guard — only trigger if corpse is truly empty after all of the above

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.CorpseLootSpan)
        {
            var lootOwner = intent.LootOwner;
            var lootOwnerGroup = intent.LootOwnerGroup;
            var corpse = intent.Corpse;

            // Phase 1: generate and distribute quest items
            DistributeQuestItems(state, ref intent);

            // Phase 2: autoloot for killer (priority) (if not killed in ActionOrchestrator by a proc/reaction)
            if (CharacterHelpers.IsAlive(_world, lootOwner) && CharacterHelpers.HasAutoLoot(_world, lootOwner))
                LootAll(lootOwner, corpse);

            // Phase 3: autoloot for rest of group
            if (lootOwnerGroup != EntityId.Invalid)
            {
                ref var group = ref _world.TryGetRef<GroupInstance>(lootOwnerGroup, out var isInGroup);
                if (isInGroup)
                {
                    foreach (var member in group.Members)
                    {
                        if (member == lootOwner) continue;
                        if (!CharacterHelpers.IsAlive(_world, member)) continue; // <- member died too
                        if (!CharacterHelpers.SameRoom(_world, intent.LootOwner, member)) continue; // <- member not anymore in same room
                        if (CharacterHelpers.HasAutoLoot(_world, member))
                            LootAll(member, corpse);
                    }
                }
            }

            // Phase 4: autosac only if corpse is truly empty
            ref var contents = ref _world.Get<ContainerContents>(intent.Corpse);
            if (contents.Items.Count == 0
                && CharacterHelpers.HasAutoSacrifice(_world, intent.LootOwner)
                && CharacterHelpers.IsAlive(_world, intent.LootOwner))
            {
                ref var sacIntent = ref _intents.AutoSacrifice.Add();
                sacIntent.Actor = intent.LootOwner;
                sacIntent.Corpse = intent.Corpse; // the corpse itself
            }
        }
    }

    private void DistributeQuestItems(GameState state, ref CorpseLootIntent intent)
    {
        //if (intent.Group == Entity.Null) return;

        //ref var group = ref killerGroup.TryGetRef<Group>(out var isInGroup);
        //if (isInGroup)
        //
        //{
        //foreach (var member in group.Members)
        //{
        //    if (!CharacterHelpers.IsAlive(member)) continue; // ← dead members don't get quest items
        //    if (!HasQuestObjective(member, intent.Corpse)) continue;

        //    var questItem = CreateQuestItem(state, member, intent.Corpse);
        //    questItem.Add(new ItemOwner { Owner = member });

        //    // if member has autoloot, go straight to inventory, otherwise land in corpse
        //    if (HasAutoloot(member))
        //        MoveToInventory(member, questItem);
        //    else
        //        AddToCorpse(intent.Corpse, questItem);
        //}
        //}
    }

    private void LootAll(EntityId looter, EntityId corpse)
    {
        ref var containerContent = ref _world.Get<ContainerContents>(corpse);
        foreach (var content in containerContent.Items.ToArray())
        {
            // get item, add to inventory, remove from corpse
            if (ItemHelpers.TryGetItemFromContainer(_world, looter, corpse, content, out var _))
            {
                _msg.To(looter).Act("You loot {0} from {1}.").With(content, corpse);

                // item looted event
                ref var itemLootedEvt = ref _itemLootedEvents.Add();
                itemLootedEvt.Entity = looter;
                itemLootedEvt.Item = content;
                itemLootedEvt.Corpse = corpse;
            }
        }
    }
}
