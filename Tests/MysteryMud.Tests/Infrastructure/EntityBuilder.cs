using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Tests.Infrastructure;

internal class EntityBuilder
{
    private readonly World _world;
    private readonly Dictionary<Type, object> _components = new();

    public EntityBuilder(World world)
    {
        _world = world;
    }

    public EntityBuilder With<T>(T component) where T : struct
    {
        _components[typeof(T)] = component;
        return this;
    }
    public EntityBuilder WithTag<T>() where T : struct
        => With(new T());
    public EntityBuilder WithName(string name)
        => With(new Name { Value = name });
    public EntityBuilder WithDescription(string description)
        => With(new Description { Value = description });
    public EntityBuilder WithLevel(int level)
        => With(new Level { Value = level });
    public EntityBuilder WithHealth(int current, int max) 
        => With(new Health { Current = current, Max = max });
    public EntityBuilder WithLocation(EntityId room)
     => With(new Location { Room = room });
    public EntityBuilder WithAutoAssist()
        => WithAuto(AutoFlags.Assist);
    public EntityBuilder WithAutoLoot()
        => WithAuto(AutoFlags.Loot);
    public EntityBuilder WithNpcAssist(AssistFlags flags)
        => With(new NpcAssistBehavior { Flags = flags });
    public EntityBuilder WithOwner(EntityId owner)
        => With(new ItemOwner { Owner = owner });
    public EntityBuilder InGroup(EntityId group)
        => With(new GroupMember { Group = group });
    // ... etc.

    //public ref T GetOrAdd<T>() where T : struct
    //{
    //    if (!_components.TryGetValue(typeof(T), out var obj))
    //    {
    //        obj = new T();
    //        _components[typeof(T)] = obj;
    //    }

    //    return ref Unsafe.As<object, T>(ref _components[typeof(T)]);
    //}

    private EntityBuilder WithAuto(AutoFlags auto)
    {
        if (_components.TryGetValue(typeof(AutoBehaviour), out var obj))
        {
            var autoBehaviour = (AutoBehaviour)obj;
            autoBehaviour.Flags |= auto;
            _components[typeof(AutoBehaviour)] = autoBehaviour;
        }
        else
        {
            _components[typeof(AutoBehaviour)] = new AutoBehaviour { Flags = auto };
        }
        return this;
    }

    public EntityId Build()
    {
        var entity = _world.CreateEntity();

        var isCharacter = _components.ContainsKey(typeof(CharacterTag));
        var isItem = _components.ContainsKey(typeof(ItemTag));

        foreach (var component in _components.Values)
        {
            _world.AddRawComponent(entity, component);

            switch (component)
            {
                case Location location:
                    if (isCharacter)
                        _world.Get<RoomContents>(location.Room).Characters.Add(entity);
                    else if (isItem)
                        _world.Get<RoomContents>(location.Room).Items.Add(entity);
                    break;

                case ContainerContents containerContents:
                    foreach (var item in containerContents.Items)
                    {
                        if (_world.Has<ContainedIn>(item))
                        {
                            ref var containedIn = ref _world.Get<ContainedIn>(item);
                            SetContainedIn(ref containedIn, entity, isItem, isCharacter);
                        }
                        else
                        {
                            var newContainedIn = new ContainedIn();
                            SetContainedIn(ref newContainedIn, entity, isItem, isCharacter);
                            _world.Add(item, newContainedIn);
                        }
                    }
                    break;
            }
        }

        return entity;
    }

    private void SetContainedIn(ref ContainedIn containedIn, EntityId container, bool isItem, bool isCharacter)
    {
        if (isItem)
        {
            containedIn.Character = EntityId.Invalid;
            containedIn.Container = container;
        }
        else if (isCharacter)
        {
            containedIn.Character = container;
            containedIn.Container = EntityId.Invalid;
        }
    }
}
