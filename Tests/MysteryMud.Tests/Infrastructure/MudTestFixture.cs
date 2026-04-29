using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Tests.Infrastructure;

internal class MudTestFixture : IDisposable
{
    public World World { get; }
    public GameState State { get; }
    public TestGameMessageService GameMessage { get; } = new();
    public TestIntentContainer Intents { get; } = new();
    public TestExperienceService TestExperienceService { get; } = new();
    public TestEventBuffer<RoomEnteredEvent> RoomEnteredEvents { get; } = new();
    public TestEventBuffer<DeathEvent> DeathEvents { get; } = new();
    public TestEventBuffer<ItemLootedEvent> ItemLootedEvents { get; } = new();
    // ... other buffers

    public MudTestFixture()
    {
        World = new World();
        State = new GameState { World = World, CurrentTick = 0, CurrentTimeMs = 0 };
    }

    // fluent entity builders
    public EntityBuilder Player(string name = "Player")
        => new EntityBuilder(World)
            .WithTag<CharacterTag>()
            .WithTag<PlayerTag>()
            .With(new CommandLevel { Value = CommandLevelKind.Player })
            //.With(new CommandBuffer())
            .WithName(name)
            .WithLevel(1)
            .With(new BaseStats { })
            .With(new EffectiveStats { })
            .With(new Form { Value = FormType.Humanoid })
            .With(new Inventory { Items = [] })
            .With(new Equipment { Slots = [] })
            .With(new CharacterEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            })
            .With(new Position { Value = PositionKind.Standing });
    public EntityBuilder Npc(string name = "Mob")
        => new EntityBuilder(World)
            .WithTag<CharacterTag>()
            .WithTag<NpcTag>()
            .With(new CommandLevel { Value = CommandLevelKind.Player })
            //.With(new CommandBuffer())
            .WithName(name)
            .WithLevel(1)
            .With(new BaseStats { })
            .With(new EffectiveStats { })
            .With(new Form { Value = FormType.Humanoid })
            .With(new Inventory { Items = [] })
            .With(new Equipment { Slots = [] })
            .With(new CharacterEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            })
            .With(new Position { Value = PositionKind.Standing })
            .With(new ThreatTable { Threat = [] });

    public EntityBuilder Room(string name = "room", string description = "a room")
        => new EntityBuilder(World)
            .WithTag<Room>()
            .WithName(name)
            .WithDescription(description)
            .With(new RoomGraph { Exits = new RoomExitValues() })
            .With(new RoomContents
            {
                Characters = [],
                Items = []
            });

    public EntityBuilder Item(string name = "item", string description = "an item")
        => new EntityBuilder(World)
            .WithTag<ItemTag>()
            .WithName(name)
            .WithLevel(1)
            .WithDescription(description)
            .With(new ItemEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            });

    public EntityBuilder Corpse(string name = "corpse", string description = "a corpse", IEnumerable<Entity> items = default!)
        => new EntityBuilder(World)
            .WithTag<ItemTag>()
            .WithName(name)
            .WithLevel(1)
            .WithDescription(description)
            .With(new ItemEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            })
            .With(new Container { Capacity = 1000 })
            .With(new ContainerContents { Items = [.. items] });

    public EntityBuilder Group()
        => new EntityBuilder(World)
            .With(new GroupInstance { Members = [] });

    public void AddGroupMembers(Entity group, params Entity[] members)
    {
        group.Get<GroupInstance>().Members.AddRange(members);
    }

    public void Dispose()
        => World.Dispose();
}
