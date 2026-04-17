using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Systems;

public sealed class LootSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<ItemLootedEvent> _itemLootedEvents;

    public LootSystem(IGameMessageService msg, IIntentContainer intents, IEventBuffer<ItemLootedEvent> itemLootedEvents)
    {
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
            var killer = intent.LootOwner;
            var killerGroup = intent.LootOwnerGroup;
            var corpse = intent.Corpse;

            // killer died during ActionOrchestrator (e.g. killed by a proc or reaction)
            if (!CharacterHelpers.IsAlive(killer))
                continue;

            // Phase 1: generate and distribute quest items
            DistributeQuestItems(state, ref intent);

            // Phase 2: autoloot for killer (priority)
            if (killer.HasAutoLoot())
                LootAll(killer, corpse);

            // Phase 3: autoloot for rest of group
            if (killerGroup != Entity.Null)
            {
                ref var group = ref killerGroup.TryGetRef<Group>(out var isInGroup);
                if (isInGroup)
                {
                    foreach (var member in group.Members)
                    {
                        if (member == killer) continue;
                        if (!CharacterHelpers.IsAlive(member)) continue; // <- member died too
                        if (!CharacterHelpers.SameRoom(intent.LootOwner, member)) continue; // <- member not anymore in same room
                        if (member.HasAutoLoot())
                            LootAll(member, corpse);
                    }
                }
            }

            // Phase 4: autosac only if corpse is truly empty
            ref var contents = ref intent.Corpse.Get<ContainerContents>();
            if (contents.Items.Count == 0
                && intent.LootOwner.HasAutoSacrifice()
                && CharacterHelpers.IsAlive(intent.LootOwner))
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

    private void LootAll(Entity looter, Entity corpse)
    {
        ref var containerContent = ref corpse.Get<ContainerContents>();
        foreach (var content in containerContent.Items.ToArray())
        {
            // get item, add to inventory, remove from corpse
            if (ItemHelpers.TryGetItemFromContainer(looter, corpse, content, out var reason))
            {
                _msg.To(looter).Send($"You loot {content.DisplayName} from {corpse.DisplayName}.");

                // item looted event
                ref var itemLootedEvt = ref _itemLootedEvents.Add();
                itemLootedEvt.Entity = looter;
                itemLootedEvt.Item = content;
                itemLootedEvt.Corpse = corpse;
            }
        }
    }
}
