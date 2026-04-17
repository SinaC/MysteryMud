using Arch.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Systems;
using MysteryMud.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.Tests;

public class LootSystemTests
{
    //private readonly MudTestFixture _f = new();
    //private readonly LootSystem _sut;

    //public LootSystemTests()
    //{
    //    _sut = new LootSystem(_f.CombatStarted, _f.Intents);
    //}

    //[Fact]
    //public void Killer_WithAutoloot_LootsAllItemsFromCorpse()
    //{
    //    // arrange
    //    var room = _f.Room();
    //    var killer = _f.Player().WithLocation(room).With(new AutoLoot()).Build();
    //    var sword = CreateItem(_f.World, "sword");
    //    var corpse = CreateCorpseWithItems(_f.World, room, [sword]);

    //    _f.Intents.CorpseLoot.Add(new CorpseLootIntent
    //    {
    //        Corpse = corpse,
    //        Killer = killer,
    //        Group = Entity.Null
    //    });

    //    _sut.Tick(_f.State);

    //    Assert.Empty(corpse.Get<ContainerContents>().Items);
    //    Assert.Contains(sword, killer.Get<Inventory>().Items);
    //}

    //[Fact]
    //public void QuestItem_GoesToQuestHolder_NotKiller()
    //{
    //    var room = _f.Room();
    //    var killer = _f.Player("Killer").WithLocation(room).With(new AutoLoot()).Build();
    //    var quester = _f.Player("Quester").WithLocation(room).With(new AutoLoot())
    //                    .With(new ActiveQuests { Quests = [questRequiringOrcKill] }).Build();
    //    var group = CreateGroup(_f.World, killer, quester);

    //    var questItem = CreateItem(_f.World, "orc_head", owner: quester); // tagged owner
    //    var sword = CreateItem(_f.World, "sword");
    //    var corpse = CreateCorpseWithItems(_f.World, room, [sword, questItem]);

    //    _f.Intents.CorpseLoot.Add(new CorpseLootIntent
    //    {
    //        Corpse = corpse,
    //        Killer = killer,
    //        Group = group
    //    });

    //    _sut.Tick(_f.State);

    //    Assert.DoesNotContain(questItem, killer.Get<Inventory>().Items);
    //    Assert.Contains(questItem, quester.Get<Inventory>().Items);
    //    Assert.Contains(sword, killer.Get<Inventory>().Items); // killer gets non-quest loot
    //}

    //[Fact]
    //public void Autosac_NotTriggered_WhenCorpseHasUnautolooted_QuestItem()
    //{
    //    var room = _f.Room();
    //    var killer = _f.Player("Killer").WithLocation(room).With(new AutoLoot()).Build();
    //    var quester = _f.Player("Quester").WithLocation(room) // no autoloot
    //                    .With(new ActiveQuests { ... }).Build();
    //    var group = CreateGroup(_f.World, killer, quester);

    //    var questItem = CreateItem(_f.World, "orc_head", owner: quester);
    //    var corpse = CreateCorpseWithItems(_f.World, room, [questItem]);

    //    _f.Intents.CorpseLoot.Add(new CorpseLootIntent
    //    {
    //        Corpse = corpse,
    //        Killer = killer,
    //        Group = group
    //    });

    //    _sut.Tick(_f.State);

    //    Assert.False(_f.AutosacIntents.Any(i => i.Corpse == corpse));
    //    Assert.Contains(questItem, corpse.Get<ContainerContents>().Items); // still there
    //}
}
