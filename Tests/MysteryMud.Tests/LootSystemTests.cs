using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Intents;
using MysteryMud.Tests.Infrastructure;
using TinyECS;

namespace MysteryMud.Tests;

public class LootSystemTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly LootSystem _sut;

    public LootSystemTests()
    {
        _sut = new LootSystem(_f.World, _f.GameMessage, _f.Intents, _f.ItemLootedEvents);
    }

    public void Dispose() => _f.Dispose();

    [Fact]
    public void Killer_WithAutoloot_LootsAllItemsFromCorpse()
    {
        // arrange
        var room = _f.Room("market").Build();
        var killer = _f.Player().WithLocation(room).WithAutoLoot().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse(items: [sword]).WithLocation(room).Build();

        _f.Intents.CorpseLoot.Add(new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = killer,
            LootOwnerGroup = EntityId.Invalid
        });

        _sut.Tick(_f.State);

        Assert.Empty(_f.World.Get<ContainerContents>(corpse).Items);
        Assert.Contains(sword, _f.World.Get<Inventory>(killer).Items);
    }

    [Fact]
    public void QuestItem_GoesToQuestHolder_NotKiller()
    {
        var group = _f.Group().Build();
        var room = _f.Room("market").Build();
        var killer = _f.Player("Killer").WithLocation(room).WithAutoLoot()
            .InGroup(group).Build();
        var quester = _f.Player("Quester").WithLocation(room).WithAutoLoot()
            .InGroup(group).Build();
        _f.AddGroupMembers(group, killer, quester);

        var questItem = _f.Item("orc_head").WithOwner(quester).Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse(items: [questItem, sword]).Build();

        _f.Intents.CorpseLoot.Add(new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = killer,
            LootOwnerGroup = group
        });

        _sut.Tick(_f.State);

        Assert.DoesNotContain(questItem, _f.World.Get<Inventory>(killer).Items);
        Assert.Contains(questItem, _f.World.Get<Inventory>(quester).Items);
        Assert.Contains(sword, _f.World.Get<Inventory>(killer).Items); // killer gets non-quest loot
    }

    [Fact]
    public void Autosac_NotTriggered_WhenCorpseHasUnautolooted()
    {
        var group = _f.Group().Build();
        var room = _f.Room("market").Build();
        var killer = _f.Player("Killer").WithLocation(room).WithAutoLoot().InGroup(group).Build();
        var quester = _f.Player("Quester").WithLocation(room) // no autoloot
            .InGroup(group).Build();
        _f.AddGroupMembers(group, killer, quester);

        var questItem = _f.Item("orc_head").WithOwner(quester).Build();
        var corpse = _f.Corpse(items: [questItem]).Build();

        _f.Intents.CorpseLoot.Add(new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = killer,
            LootOwnerGroup = group
        });

        _sut.Tick(_f.State);

        Assert.False(_f.Intents.AutoSacrifice.Any(i => i.Corpse == corpse));
        Assert.Contains(questItem, _f.World.Get<ContainerContents>(corpse).Items); // still there
    }
}
