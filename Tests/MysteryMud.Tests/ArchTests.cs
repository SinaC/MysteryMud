using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Tests;

public class ArchTests
{
    [Fact]
    public void Arch_DoesNotModify_ExternalList_OnArchetypeMigration()
    {
        var world = World.Create();

        // create two entities with a component
        var e1 = world.Create(new CombatState { Target = Entity.Null });
        var e2 = world.Create(new CombatState { Target = Entity.Null });

        // maintain our own list — simulating RoomContents.Characters
        var list = new List<Entity> { e1, e2 };

        // snapshot list contents before migration
        var beforeCount = list.Count;
        var beforeE1 = list[0];
        var beforeE2 = list[1];

        // trigger archetype migration
        e1.Remove<CombatState>();

        // check if list was affected
        Assert.Equal(beforeCount, list.Count);   // still 2?
        Assert.Equal(beforeE1, list[0]);         // still e1?
        Assert.Equal(beforeE2, list[1]);         // still e2?

        World.Destroy(world);
    }

    [Fact]
    public void Arch_Corrupts_Entity_OnRemoveNotExistingComponent_StoredInList()
    {
        var world = World.Create();

        // create 4 entities
        var e1 = world.Create(new Name { Value = "entity1" });
        var e2 = world.Create(new Name { Value = "entity2" });
        var e3 = world.Create(new Name { Value = "entity3" });
        var e4 = world.Create(new Name { Value = "entity4" });

        // maintain our own list
        var list = new List<Entity> { e1, e2, e3, e4 };

        // snapshot list contents before migration
        var snapshot = list.ToArray();
        foreach (var e in snapshot)
        {
            e.Remove<CombatState>();
        }

        Assert.Equal("entity4", e1.Get<Name>().Value);
        Assert.Equal("entity4", e2.Get<Name>().Value);
        Assert.Equal("entity4", e3.Get<Name>().Value);
        Assert.Equal("entity4", e4.Get<Name>().Value);
    }

    [Fact]
    public void Arch_DoesntCorrupt_Entity_OnRemoveExistingComponent_StoredInList()
    {
        var world = World.Create();

        // create 4 entities
        var e1 = world.Create(new Name { Value = "entity1" }, new CombatState());
        var e2 = world.Create(new Name { Value = "entity2" }, new CombatState());
        var e3 = world.Create(new Name { Value = "entity3" }, new CombatState());
        var e4 = world.Create(new Name { Value = "entity4" }, new CombatState());

        // maintain our own list
        var list = new List<Entity> { e1, e2, e3, e4 };

        // snapshot list contents before migration
        var snapshot = list.ToArray();
        foreach (var e in snapshot)
        {
            e.Remove<CombatState>();
        }

        Assert.Equal("entity1", e1.Get<Name>().Value);
        Assert.Equal("entity2", e2.Get<Name>().Value);
        Assert.Equal("entity3", e3.Get<Name>().Value);
        Assert.Equal("entity4", e4.Get<Name>().Value);
    }

    [Fact]
    public void Arch_Corrupts_Entity_OnRemoveNotExistingComponent()
    {
        var world = World.Create();

        // create 4 entities
        var e1 = world.Create(new Name { Value = "entity1" });
        var e2 = world.Create(new Name { Value = "entity2" });
        var e3 = world.Create(new Name { Value = "entity3" });
        var e4 = world.Create(new Name { Value = "entity4" });

        e1.Remove<CombatState>();
        e2.Remove<CombatState>();
        e3.Remove<CombatState>();
        e4.Remove<CombatState>();

        Assert.Equal("entity4", e1.Get<Name>().Value);
        Assert.Equal("entity4", e2.Get<Name>().Value);
        Assert.Equal("entity4", e3.Get<Name>().Value);
        Assert.Equal("entity4", e4.Get<Name>().Value);
    }

    [Fact]
    public void Arch_DoesntCorrupt_Entity_OnRemoveExistingComponent()
    {
        var world = World.Create();

        // create 4 entities
        var e1 = world.Create(new Name { Value = "entity1" }, new CombatState());
        var e2 = world.Create(new Name { Value = "entity2" }, new CombatState());
        var e3 = world.Create(new Name { Value = "entity3" }, new CombatState());
        var e4 = world.Create(new Name { Value = "entity4" }, new CombatState());

        e1.Remove<CombatState>();
        e2.Remove<CombatState>();
        e3.Remove<CombatState>();
        e4.Remove<CombatState>();

        Assert.Equal("entity1", e1.Get<Name>().Value);
        Assert.Equal("entity2", e2.Get<Name>().Value);
        Assert.Equal("entity3", e3.Get<Name>().Value);
        Assert.Equal("entity4", e4.Get<Name>().Value);
    }

}