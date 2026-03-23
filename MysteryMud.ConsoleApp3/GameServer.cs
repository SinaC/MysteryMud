using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Infrastructure;
using MysteryMud.ConsoleApp3.Infrastructure.Network;
using MysteryMud.ConsoleApp3.Infrastructure.Services;

namespace MysteryMud.ConsoleApp3;

public class GameServer
{
    private readonly World _world;
    private readonly ConnectionService _connections;
    private readonly TelnetServer _telnet;
    private readonly MessageService _messageService;
    private readonly CommandBus _commandBus;
    private readonly MessageBus _messageBus;
    private readonly Scheduler _scheduler;
    private readonly GameLoop _gameLoop;

    public GameServer()
        : this(World.Create())
    {
    }

    public GameServer(World world)
    {
        _world = world;
        _connections = new ConnectionService(_world);
        _telnet = new TelnetServer(
            port: 4000,
            onInputReceived: HandleInputReceived,
            onConnected: HandleConnected,
            onDisconnected: HandleDisconnected
        );

        _messageService = new MessageService(_telnet);

        _commandBus = new CommandBus();
        _messageBus = new MessageBus(_messageService);
        _scheduler = new Scheduler();

        _gameLoop = new GameLoop(_messageService, _commandBus, _messageBus, _scheduler, _world);
    }

    public void Start()
    {
        Task.Run(() => _telnet.Start());

        _gameLoop.Run();
    }

    private void HandleInputReceived(int connectionId, ReadOnlySpan<char> input)
    {
        // TODO: check player state (login, character creation, playing, disconnected, ...)
        // for now we just handle everything as a command, but eventually we will want to handle login and character creation separately

        if (_connections.TryGetEntity(connectionId, out var entity))
        {
            // TODO: detect if the command is a login command and handle that separately, for now we just enqueue everything to the command system and it can handle it from there
            _commandBus.Publish(entity, input);
        }
    }

    private void HandleConnected(int connectionId) // TODO: rename HandleNewConnection
    {
        Logger.Logger.System(LogLevel.Information, "Handling new connection with id {ConnectionId}", connectionId);

        var entity = _connections.CreatePlayer(connectionId);

        // TODO
        InitializePlayer(entity, connectionId);
    }

    private void HandleDisconnected(int connectionId)
    {
        Logger.Logger.System(LogLevel.Information, "Handling disconnection for connection id {ConnectionId}", connectionId);

        if (!_connections.TryGetEntity(connectionId, out var entity))
            return;

        //// Optional: announce to room
        //var room = entity.Get<Position>().Room;

        //ActSystem.ToOthers(_world, room, entity,
        //    $"{entity.Get<Name>().Value} has disconnected.");

        //// Remove from room
        //room.Get<RoomContents>().Characters.Remove(entity);

        // mark as disconnected and let the cleanup system handle the actual destruction and cleanup of the entity, this allows us to still show the character in the room for a short time after disconnecting, and also allows us to handle any necessary cleanup in a more controlled way
        if (!entity.Has<DisconnectedTag>())
            entity.Add<DisconnectedTag>();

        // remove from connection service
        _connections.Remove(connectionId);
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
        player.Add(new Location { Room = RoomFactory.StartingRoomEntity });
        player.Add(new PositionComponent { Position = Position.Standing });
        player.Add<DirtyStats>(); // ensure stats are recomputed
        RoomFactory.StartingRoomEntity.Get<RoomContents>().Characters.Add(player); // move to starting room

        _telnet.Write(connectionId, "Welcome to the game!\r\n> ");
        _telnet.Flush(connectionId);
    }

    //private class LoginState
    //{
    //    public string Name { get; set; }
    //    public string Password { get; set; } // TODO: encryption
    //}

    //private class CharacterCreationState
    //{
    //    // TODO
    //}

    //// ----------------------------------------------------
    //// NANNY
    //// ----------------------------------------------------
    //// TODO: refactor
    //private void HandleNanny(ReadOnlySpan<char> input)
    //{
    //    _tempName = "joel";
    //    SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO);
    //    NannyState = NannyState.Finished;
    //    InitializePlayer();
    //    return;

    //    // TODO

    //    switch (NannyState)
    //    {
    //        case NannyState.NewConnection:
    //            SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO); // Enable local echo
    //            Write("Welcome to the MUD!\r\nPlease enter your name: ");
    //            Flush();
    //            NannyState = NannyState.EnterName;
    //            break;

    //        case NannyState.EnterName:
    //            // COLOR TESTING
    //            //ColorTest(ColorMode.TrueColor);
    //            //ColorTest(ColorMode.ANSI256);
    //            //ColorTest(ColorMode.ANSI16);
    //            //ColorTest(ColorMode.None);
    //            //ColorTest();

    //            _tempName = input.ToString().Trim();
    //            Write("Enter your password: ");
    //            SendCmd(Telnet.WILL, Telnet.TELOPT_ECHO); // Disable local echo for password input
    //            Flush();
    //            NannyState = NannyState.EnterPassword;
    //            break;

    //        case NannyState.EnterPassword:
    //            _tempPassword = input.ToString();
    //            // Here you could verify password from a database or file
    //            Write("Confirm password: ");
    //            Flush();
    //            NannyState = NannyState.ConfirmPassword;
    //            break;

    //        case NannyState.ConfirmPassword:
    //            if (_tempPassword != input.ToString())
    //            {
    //                Write("Passwords do not match. Enter password: ");
    //                Flush();
    //                NannyState = NannyState.EnterPassword;
    //            }
    //            else
    //            {
    //                Write("Character creation complete!\r\n");
    //                SendCmd(Telnet.WONT, Telnet.TELOPT_ECHO); // Enable local echo
    //                Flush();
    //                NannyState = NannyState.Finished;

    //                // Attach default ECS components
    //                InitializePlayer();
    //            }
    //            break;
    //    }
    //}
}