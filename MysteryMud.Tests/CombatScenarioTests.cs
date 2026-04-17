using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.Tests;

public class CombatScenarioTests
{
    //private readonly MudTestFixture _f = new();

    //[Fact]
    //public void GroupKill_GrantsXpToAllMembers_AndLootsCorrectly()
    //{
    //    // full pipeline: KillCommand -> DeathSystem -> LootSystem -> XP
    //    var room = _f.Room("Temple").Build();
    //    var alice = _f.Player("Alice").WithLocation(room).WithAutoAssist().WithAutoLoot()
    //                   .With(new Progression { Level = 5 }).Build();
    //    var bob = _f.Player("Bob").WithLocation(room).WithAutoAssist().WithAutoLoot()
    //                   .With(new Progression { Level = 5 }).Build();
    //    var group = CreateGroup(_f.World, alice, bob);
    //    var orc = _f.Npc("Orc").WithLocation(room)
    //                   .WithHealth(1, 100) // will die in one hit
    //                   .With(new ExperienceValue { Amount = 100 }).Build();

    //    // alice kills orc
    //    _killCommand.Execute(_f.State, alice, "kill", "orc");
    //    _f.RunTick(); // AutoAssist, AutoAttack, ActionOrchestrator, DeathSystem, LootSystem

    //    Assert.True(orc.Has<Dead>());
    //    Assert.True(alice.Get<Progression>().Experience > 0);
    //    Assert.True(bob.Get<Progression>().Experience > 0);   // shared XP
    //    Assert.Empty(orc.Get<Inventory>().Items);              // looted
    //}
}
