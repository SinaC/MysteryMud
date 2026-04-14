using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Extensions;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class SocialCommand : IExplicitCommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;

    private readonly SocialDefinition _socialDefinition;

    public CommandDefinition Definition { get; }

    public SocialCommand(ILogger logger, IGameMessageService msg, SocialDefinition socialDefinition)
    {
        _logger = logger;
        _msg = msg;
        _socialDefinition = socialDefinition;

        Definition = new CommandDefinition
        {
            Id = socialDefinition.Name.ComputeUniqueId(),
            Name = socialDefinition.Name,
            Aliases = [],
            CannotBeForced = false,
            RequiredLevel = CommandLevelKind.Player,
            MinimumPosition = PositionKind.Resting,
            Priority = -1, // low priority -> unsure every other commands has more priority than social command
            DisallowAbbreviation = false,
            HelpText = string.Empty,
            Syntaxes = ["[cmd]", "[cmd] <character>"],
            Categories = ["social"],
            ThrottlingCategories = CommandThrottlingCategories.Social
        };
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        var useCharacterNotFound = false;
        Entity? victim = null;

        if (ctx.TargetCount > 0)
        {
            ref var room = ref actor.Get<Location>().Room;
            ref var roomContents = ref room.Get<RoomContents>();

            victim = EntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents.Characters);
            if (victim == null)
            {
                if (_socialDefinition.CharacterNotFound is null)
                    _msg.To(actor).Send("They aren't here.");
                else
                {
                    if (_socialDefinition.CharacterNotFound.Contains("{0"))
                    {
                        _logger.LogError("Social {name} CharacterNotFound phrase contains arguments.", _socialDefinition.Name);
                        _msg.To(actor).Send("They aren't here.");
                    }
                    useCharacterNotFound = true;
                }
            }
        }

        //
        if (useCharacterNotFound)
            _msg.To(actor).Act(_socialDefinition.CharacterNotFound!).With(actor);
        else if (victim == null)
        {
            if (_socialDefinition.CharacterNoArg is not null)
                _msg.To(actor).Act(_socialDefinition.CharacterNoArg).With(actor);
            if (_socialDefinition.OthersNoArg is not null)
                _msg.ToRoom(actor).Act(_socialDefinition.OthersNoArg).With(actor);
        }
        else if (victim == actor)
        {
            if (_socialDefinition.CharacterAuto is not null)
                _msg.To(actor).Act(_socialDefinition.CharacterAuto).With(actor);
            if (_socialDefinition.OthersAuto is not null)
                _msg.ToRoom(actor).Act(_socialDefinition.OthersAuto).With(actor);
        }
        else
        {
            if (_socialDefinition.CharacterFound is not null)
                _msg.To(actor).Act(_socialDefinition.CharacterFound).With(actor, victim);
            if (_socialDefinition.VictimFound is not null)
                _msg.To(victim.Value).Act(_socialDefinition.VictimFound).With(actor, victim);
            if (_socialDefinition.OthersFound is not null)
                _msg.ToRoomExcept(actor, victim.Value).Act(_socialDefinition.OthersFound).With(actor, victim);

            // TODO
            // trigger mob program
            //(Victim as INonPlayableCharacter)?.OnSocial(Actor, SocialDefinition);
        }
    }
}
