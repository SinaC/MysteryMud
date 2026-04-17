using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Tests.Infrastructure;

internal class MudTestFixture : IDisposable
{
    public World World { get; } = World.Create();
    public GameState State { get; }
    public TestIntentContainer Intents { get; } = new();
    public TestEventBuffer<RoomEnteredEvent> RoomEnteredEvents { get; } = new();
    public TestEventBuffer<DeathEvent> DeathEvents { get; } = new();
    // ... other buffers

    public MudTestFixture()
    {
        State = new GameState { World = World, CurrentTick = 0, CurrentTimeMs = 0 };
    }

    // fluent entity builders
    public EntityBuilder Player(string name = "Player")
        => new EntityBuilder(World)
            .WithTag<CharacterTag>()
            .WithTag<PlayerTag>()
            .With(new CommandLevel { Value = CommandLevelKind.Player })
            .With(new CommandBuffer())
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
            .With(new CommandBuffer())
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
            .With(new RoomGraph { Exits = [] })
            .With(new RoomContents
            {
                Characters = [],
                Items = []
            });

    public void Dispose() => World.Destroy(World);
}

public class EntityBuilder
{
    private readonly World _world;
    private readonly List<object> _components = new();

    public EntityBuilder(World world)
    {
        _world = world;
    }

    public EntityBuilder With(object component) { _components.Add(component); return this; }
    public EntityBuilder WithTag<T>() where T : struct { _components.Add(new T()); return this; }
    public EntityBuilder WithName(string name) { _components.Add(new Name { Value = name }); return this; }
    public EntityBuilder WithDescription(string name) { _components.Add(new Description { Value = name }); return this; }
    public EntityBuilder WithLevel(int level) { _components.Add(new Level { Value = level }); return this; }
    public EntityBuilder WithHealth(int current, int max) { _components.Add(new Health { Current = current, Max = max }); return this; }
    public EntityBuilder WithLocation(Entity room) { _components.Add(new Location { Room = room }); return this; }
    public EntityBuilder WithAutoAssist() { _components.Add(new AutoAssist()); return this; }
    public EntityBuilder WithNpcAssist(AssistFlags flags) { _components.Add(new NpcAssistBehavior { Flags = flags }); return this; }
    public EntityBuilder InGroup(Entity group) { _components.Add(new GroupMember { Group = group }); return this; }
    // ... etc.

    public Entity Build()
    {
        // create entity with all components
        var entity = _world.Create();

        var isCharacter = _components.Any(x => x is CharacterTag);
        var isItem = _components.Any(x => x is ItemTag);
        foreach (var component in _components)
        {
            entity.Add(component);
            if (component is Location location)
            {
                if (isCharacter)
                    location.Room.Get<RoomContents>().Characters.Add(entity);
                else if (isItem)
                    location.Room.Get<RoomContents>().Items.Add(entity);
            }
        }

        return entity;
    }
}
