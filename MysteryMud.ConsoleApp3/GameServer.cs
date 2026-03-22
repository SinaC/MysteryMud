using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Events;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Network;
using MysteryMud.ConsoleApp3.Services;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3;

public class GameServer
{
    private readonly World _world;
    private readonly ConnectionService _connections;
    private readonly TelnetServer _telnet;
    private readonly IMessageService _messageService;

    public GameServer() : this(World.Create())
    {
    }

    public GameServer(World world)
    {
        _world = world;
        _connections = new ConnectionService(_world);
        _telnet = new TelnetServer(
            port: 4000,
            onCommand: HandleCommand,
            onLogin: HandleLogin
        );

        _messageService = new MessageService(_telnet);
        Services.Services.Messages = _messageService; // static service locator for now, can refactor to proper dependency injection later
    }

    public void Start()
    {
        Task.Run(() => _telnet.Start());

        RunGameLoop();
    }

    private void RunGameLoop()
    {
        while (true)
        {
            Tick();

            Thread.Sleep(100); // tick rate
        }
    }

    private void Tick()
    {
        TimeSystem.NextTick();

        //Console.WriteLine($"Tick: {TimeSystem.CurrentTick}");

        // process player commands
        CommandSystem.ProcessCommands(_world);

        // process scheduled events
        EventScheduler.ProcessEvents(_world, TimeSystem.CurrentTick);
        // handle state transitions for effects
        //StateMachineSystem.Update(world); TODO: implement state machine system and handle with scheduled events

        // AiSystem.Process(world);
        // handle combat rounds
        CombatSystem.Process(_world);

        // handle deaths and related consequences
        DeathSystem.Process(_world);

        // handle player deaths and respawns
        RespawnSystem.RespawnPlayers(_world);

        // recalculate stats for entities
        StatSystem.Recalculate(_world);

        // perform cleanup tasks like removing characters, items, ...
        CleanupSystem.Cleanup(_world);

        // send output to players
        FlushOutputSystem.FlushOutputs(_world);
    }

    private void HandleCommand(int connectionId, ReadOnlySpan<char> command)
    {
        var entity = _connections.GetEntity(connectionId);

        // TODO: detect if the command is a login command and handle that separately, for now we just enqueue everything to the command system and it can handle it from there
        CommandSystem.Enqueue(entity, command);
    }

    private void HandleLogin(int connectionId, ReadOnlySpan<char> command) // TODO: rename HandleNewConnection
    {
        var entity = _connections.CreatePlayer(connectionId);

        // TODO
        InitializePlayer(entity, connectionId);
    }


    private void InitializePlayer(Entity player, int connectionId)
    {
        // TODO: fill in with actual character creation data, load from file, etc
        player.Add(new PlayerTag());
        player.Add(new Name { Value = "joel" }); // RODO: implement character creation and loading from file, for now just use a placeholder name
        player.Add(new BaseStats
        {
            Level = 1,
            Experience = 0,
            Values = new Dictionary<StatType, int>
            {
                [StatType.Strength] = 15,
                [StatType.Intelligence] = 10,
                [StatType.Wisdom] = 15,
                [StatType.Dexterity] = 12,
                [StatType.Constitution] = 15,
                [StatType.HitRoll] = 0,
                [StatType.DamRoll] = 0,
                [StatType.Armor] = 0
            }
        });
        player.Add(new EffectiveStats
        {
            Level = 1,
            Experience = 0,
            Values = new Dictionary<StatType, int>
            {
                [StatType.Strength] = 15,
                [StatType.Intelligence] = 10,
                [StatType.Wisdom] = 15,
                [StatType.Dexterity] = 12,
                [StatType.Constitution] = 15,
                [StatType.HitRoll] = 0,
                [StatType.DamRoll] = 0,
                [StatType.Armor] = 0
            }
        });
        player.Add(new Health { Current = 100, Max = 100 });
        player.Add(new Inventory { Items = [] });
        player.Add(new Equipment { Slots = [] });
        player.Add(new CharacterEffects
        {
            Effects = [],
            EffectsByTag = new Entity?[32]
        });
        player.Add(new Position { Room = WorldFactory.StartingRoomEntity });
        player.Add<DirtyStats>(); // ensure stats are recomputed
        WorldFactory.StartingRoomEntity.Get<RoomContents>().Characters.Add(player); // move to starting room

        _telnet.Write(connectionId, "Welcome to the game!\r\n> ");
        _telnet.Flush(connectionId);
    }
}