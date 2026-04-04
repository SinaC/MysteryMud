using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Dispatching;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Effect;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Eventing;
using MysteryMud.Infrastructure.Intent;
using MysteryMud.Infrastructure.Network;
using MysteryMud.Infrastructure.Scheduler;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.ConsoleApp.Hosting;

public class GameServer
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly ICommandDispatcher _commandDispatcher;

    private readonly ConnectionService _connections;
    private readonly TelnetServer _telnet;
    private readonly OutputService _outputService;
    private readonly CommandBus _commandBus;
    private readonly MessageBus _messageBus;
    private readonly Scheduler _scheduler;
    private readonly ActService _actService;
    private readonly GameMessageService _gameMessageService;
    private readonly IntentBusContainer _intentBusContainer;
    private readonly GameLoop _gameLoop;

    public GameServer(ILogger logger, World world, ICommandDispatcher commandDispatcher, SpellDatabase spellDatabase, EffectRegistry effectRegistry)
    {
        _world = world;
        _logger = logger;
        _commandDispatcher = commandDispatcher;

        _connections = new ConnectionService(_world);
        _telnet = new TelnetServer(
            port: 4000,
            onInputReceived: HandleInputReceived,
            onConnected: HandleConnected,
            onDisconnected: HandleDisconnected
        );
        _outputService = new OutputService(_logger, _telnet);
        _commandBus = new CommandBus(_commandDispatcher);
        _messageBus = new MessageBus(_outputService);
        _scheduler = new Scheduler(_logger);
        _actService = new ActService();
        _gameMessageService = new GameMessageService(_messageBus, _actService);
        _intentBusContainer = new IntentBusContainer();
        _gameLoop = new GameLoop(_logger, _outputService, _commandBus, _messageBus, _scheduler, _gameMessageService, _intentBusContainer, spellDatabase, effectRegistry, _world);
    }

    public void Start()
    {
        _logger.LogInformation(LogEvents.System, "Starting game server");

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
        _logger.LogInformation(LogEvents.System,"Handling new connection with id {ConnectionId}", connectionId);

        var entity = _connections.CreatePlayer(connectionId);

        // TODO
        InitializePlayer(entity, connectionId);
    }

    private void HandleDisconnected(int connectionId)
    {
        _logger.LogInformation(LogEvents.System,"Handling disconnection for connection id {ConnectionId}", connectionId);

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
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        player.Add(
            new CharacterTag(),
            new PlayerTag(),
            new CommandLevel { Value = CommandLevelKind.Admin },
            new CommandBuffer(),
            commandThrottle,
            new Name { Value = "joel" }, // TODO: implement character creation and loading from file, for now just use a placeholder name
            new BaseStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatKind, int>
                {
                    [StatKind.Strength] = 15,
                    [StatKind.Intelligence] = 10,
                    [StatKind.Wisdom] = 15,
                    [StatKind.Dexterity] = 12,
                    [StatKind.Constitution] = 15,
                    [StatKind.HitRoll] = 0,
                    [StatKind.DamRoll] = 0,
                    [StatKind.ArmorClass] = 0
                }
            },
            new EffectiveStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatKind, int>
                {
                    [StatKind.Strength] = 15,
                    [StatKind.Intelligence] = 10,
                    [StatKind.Wisdom] = 15,
                    [StatKind.Dexterity] = 12,
                    [StatKind.Constitution] = 15,
                    [StatKind.HitRoll] = 0,
                    [StatKind.DamRoll] = 0,
                    [StatKind.ArmorClass] = 0
                }
            },
            new Health { Current = 100000, Max = 100000 },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
            new Location { Room = RoomFactory.StartingRoomEntity },
            new Position { Value = PositionKind.Standing },
            new DirtyStats()); // ensure stats are recomputed
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