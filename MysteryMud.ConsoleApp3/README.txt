Entity/Components

Character
  ├Location: room
  ├BaseStats: level, experience, dictionary stat/value
  ├EffectiveStats: dictionary stat/value
  ├CharacterEffects: effect list, effects by tag, active tags
  ├Inventory: list of items
  ├Equipment: list of equipped items
  └Health: current and max health
  optional
    DirtyStats: needs effective stats recalculated
    CombatState: in combat
    DeadTag: is dead
    Gender: male|female|neutral
    Mana: current and max mana

Npc(character+)
  ├NpcTag
  └ThreatTable: list of characters and threat values for aggro

Player(character+)
  ├PlayerTag
  └Connection
  optional
    RespawnState: respawn timer and location when a player dies
    DisconnectedTag: is disconnected

Item
    todo
Room
    todo
Zone
    todo

Effect (not stacking if difference source)
 ├ EffectInstance: Source, Target, Template, StackCount
 ├ Duration: StartTick, ExpiredTick
 ├ EffectTag: EffectTagId
 ├ StatModifiers: StatModifier list
 ├ DamageOverTime: Damage, DamageType, TickRate, NextTick;
 └ HealOverTime: Heal, TickRate, NextTick;

Datas

EffectTemplate:
    Name: name of the effect template (e.g. "Strength Buff")
    EffectTag: EffectTagId
    StackingRule: None|Replace|ExtendDuration|ReplaceIfStronger
    AffectFlags: bitflags for quick checks (e.g. is buff, is debuff, is dispellable) (TODO)
    MaxStacks: maximum number of stacks (if stacking is allowed)
    StatModifiers: StatModifierDefinition list
    DotFunction: function to calculate damage for damage over time effects (returns DotDefinition)
    HotFunction: function to calculate healing for heal over time effects (returns HotDefinition)
    ApplyMessage: message to show when the effect is applied
    WearOffMessage: message to show when the effect wears off

******************************************************************
** replace Location, RoomContents, Inventory, ContainerContents **
** with uniformied ContainerContents and ContainedIn component  **
******************************************************************

1. Core Idea: Unified "Location"

Instead of special cases:

RoomContents
Inventory
Container

We represent everything that can contain entities with a single component:

public struct ContainedIn
{
    public Entity Parent;
}

Examples:

Entity	Parent
Player	Room
Sword	Room
Potion	Player
Gem	Chest

This creates a tree structure.

Example world:

Room 1000
 ├─ Player
 │   ├─ Sword
 │   └─ Potion
 └─ Chest
     └─ Gem
2. Container Component

Not every entity can hold items.

public struct Container
{
    public int Capacity;
}

Examples:

Entity	Container
Room	yes
Player	yes
Chest	yes
Sword	no
3. Fast Container Cache (Important)

Like RoomContents, we maintain a cache of children.

public struct ContainerContents
{
    public List<Entity> Entities;
}

Now any container can be queried quickly.

4. Containment System

Central system that moves entities between containers.

public static class ContainmentSystem
{
    public static void Move(
        World world,
        Entity entity,
        Entity newParent)
    {
        ref var contained = ref world.Get<ContainedIn>(entity);

        var oldParent = contained.Parent;

        if (world.Has<ContainerContents>(oldParent))
            world.Get<ContainerContents>(oldParent).Entities.Remove(entity);

        if (world.Has<ContainerContents>(newParent))
            world.Get<ContainerContents>(newParent).Entities.Add(entity);

        contained.Parent = newParent;
    }
}

This system is used by:

movement

inventory

containers

dropping items

5. Room Setup

Rooms are containers.

var room = world.Create(
    new Room { Vnum = 1000 },
    new Container(),
    new ContainerContents { Entities = new List<Entity>() });
6. Player Setup

Players are containers (inventory).

var player = world.Create(
    new Character(),
    new Location { Room = room },
    new Container(),
    new ContainerContents { Entities = new List<Entity>() });
7. Chest Setup
var chest = world.Create(
    new Item(),
    new Container { Capacity = 20 },
    new ContainerContents { Entities = new List<Entity>() },
    new ContainedIn { Parent = room });
8. Item Setup
var sword = world.Create(
    new Item(),
    new Name { Value = "sword" },
    new ContainedIn { Parent = room });
9. Get Command
public class GetCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        Entity source;

        if (ctx.Secondary.Name.IsEmpty)
        {
            source = actor.Get<Location>().Room;
        }
        else
        {
            source = FindContainer(world, actor, ctx.Secondary);
        }

        var contents = world.Get<ContainerContents>(source);

        foreach (var e in TargetResolver.Resolve(actor, ctx.Primary, contents.Entities))
        {
            ContainmentSystem.Move(world, e, actor);

            Console.WriteLine($"You get {e.Get<Name>().Value}");
        }
    }
}
10. Put Command
public class PutCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = world.Get<ContainerContents>(actor);

        var container = FindContainer(world, actor, ctx.Secondary);

        foreach (var item in TargetResolver.Resolve(actor, ctx.Primary, inventory.Entities))
        {
            ContainmentSystem.Move(world, item, container);

            Console.WriteLine($"You put {item.Get<Name>().Value} in container.");
        }
    }
}
11. Look In Command
public class LookInCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        var container = FindContainer(world, actor, ctx.Primary);

        var contents = world.Get<ContainerContents>(container);

        Console.WriteLine("It contains:");

        foreach (var e in contents.Entities)
            Console.WriteLine($" - {e.Get<Name>().Value}");
    }
}
12. Why This Design Is Powerful

Everything becomes the same concept:

Location	Implementation
Room	container
Inventory	container
Bag	container
Chest	container
Corpse	container

So commands work everywhere:

get sword from chest
get sword from corpse
get sword from bag
put gem in bag
put gem in chest

No special logic required.

13. Nested Containers Work Automatically

Example:

Player
 └─ Bag
     └─ SmallBox
         └─ Ring

All valid:

get ring from smallbox
get ring from bag
put ring in smallbox
14. Performance

Lookups are fast because we use cached lists:

ContainerContents.Entities

Room lookups:

10–30 entities

Inventory:

5–20 entities

Container:

0–10 entities

Very cheap.

15. ECS Component Set for Items

Typical item components:

Item
Name
Description

ContainedIn

Container
ContainerContents

EquipmentSlot
EquippedBy
16. Final Hierarchy

Your world becomes a tree of containment:

Room
 ├─ Player
 │   ├─ Sword
 │   ├─ Potion
 │   └─ Bag
 │        └─ Gem
 └─ Chest
      └─ Gold
17. Result

You now have:

Unified location system

Nested containers

Fast lookups

Clean ECS architecture

Simple command logic

This is much cleaner than classic ROM/Diku container code.



public class GiveCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = world.Get<ContainerContents>(actor);
        var room = actor.Get<Location>().Room;

        // Find target character in room
        var characters = Spatial.GetCharactersInRoom(world, room);
        var target = TargetResolver.ResolveSingle(actor, ctx.Secondary, characters);
        if (target == default)
        {
            Console.WriteLine("They are not here.");
            return;
        }

        // Move item
        foreach (var item in TargetResolver.Resolve(actor, ctx.Primary, inventory.Entities))
        {
            // Unequip if necessary
            if (world.Has<Equipped>(item))
            {
                var equipped = world.Get<Equipped>(item);
                EquipmentSystem.Unequip(world, actor, equipped.Slot);
            }

            ContainmentSystem.Move(world, item, target);
            Console.WriteLine($"You give {item.Get<Name>().Value} to {target.Get<Name>().Value}.");
        }
    }
}

public class DropCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = world.Get<ContainerContents>(actor);
        var room = actor.Get<Location>().Room;

        foreach (var item in TargetResolver.Resolve(actor, ctx.Primary, inventory.Entities))
        {
            // Unequip if necessary
            if (world.Has<Equipped>(item))
            {
                var equipped = world.Get<Equipped>(item);
                EquipmentSystem.Unequip(world, actor, equipped.Slot);
            }

            ContainmentSystem.Move(world, item, room);
            Console.WriteLine($"You drop {item.Get<Name>().Value}.");
        }
    }
}

public class GetCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        // Determine source container
        Entity source;
        if (ctx.Secondary.Name.IsEmpty)
        {
            // default: room
            source = actor.Get<Location>().Room;
        }
        else
        {
            source = FindContainer(world, actor, ctx.Secondary);
            if (source == default)
            {
                Console.WriteLine("You don't see that here.");
                return;
            }
        }

        var contents = world.Get<ContainerContents>(source);

        // Resolve target items
        foreach (var item in TargetResolver.Resolve(actor, ctx.Primary, contents.Entities))
        {
            ContainmentSystem.Move(world, item, actor); // move to inventory
            Console.WriteLine($"You get {item.Get<Name>().Value}.");
        }
    }

    private Entity FindContainer(World world, Entity actor, TargetArg containerArg)
    {
        // Search in room first
        var room = actor.Get<Location>().Room;
        var roomContents = world.Get<ContainerContents>(room);

        foreach (var e in TargetResolver.Resolve(actor, containerArg, roomContents.Entities))
            return e;

        // Then inventory
        var inventory = world.Get<ContainerContents>(actor);
        foreach (var e in TargetResolver.Resolve(actor, containerArg, inventory.Entities))
            return e;

        return default;
    }
}

public class PutCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = world.Get<ContainerContents>(actor);

        var container = FindContainer(world, actor, ctx.Secondary);

        foreach (var item in TargetResolver.Resolve(actor, ctx.Primary, inventory.Entities))
        {
            ContainmentSystem.Move(world, item, container);

            Console.WriteLine($"You put {item.Get<Name>().Value} in container.");
        }
    }
}

public class LookInCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(SystemContext ctx, GameState gameState, Entity actor, CommandContext ctx)
    {
        var container = FindContainer(world, actor, ctx.Primary);

        var contents = world.Get<ContainerContents>(container);

        Console.WriteLine("It contains:");

        foreach (var e in contents.Entities)
            Console.WriteLine($" - {e.Get<Name>().Value}");
    }
}

public static class ContainmentSystem
{
    public static void Move(
        World world,
        Entity entity,
        Entity newParent)
    {
        ref var contained = ref world.Get<ContainedIn>(entity);

        var oldParent = contained.Parent;

        if (world.Has<ContainerContents>(oldParent))
            world.Get<ContainerContents>(oldParent).Entities.Remove(entity);

        if (world.Has<ContainerContents>(newParent))
            world.Get<ContainerContents>(newParent).Entities.Add(entity);

        contained.Parent = newParent;
    }
}