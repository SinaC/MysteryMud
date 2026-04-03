using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class SocialCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly ILogger _logger;
    private readonly SocialDefinition _socialDefinition;

    public CommandDefinition Definition { get; }

    public SocialCommand(ILogger logger, SocialDefinition socialDefinition)
    {
        _logger = logger;
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
            AllowAbbreviation = true,
            HelpText = string.Empty,
            Syntaxes = ["[cmd]", "[cmd] <character>"],
            Categories = ["social"],
            ThrottlingCategories = CommandThrottlingCategories.Social
        };
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        var useCharacterNotFound = false;
        Entity victim = default;

        if (ctx.TargetCount > 0)
        {
            ref var room = ref actor.Get<Location>().Room;
            ref var roomContents = ref room.Get<RoomContents>();

            victim = EntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents.Characters);
            if (victim == default)
            {
                if (_socialDefinition.CharacterNotFound is null)
                    systemContext.Msg.To(actor).Send("They aren't here.");
                else
                {
                    if (_socialDefinition.CharacterNotFound.Contains("{0"))
                    {
                        _logger.LogError("Social {name} CharacterNotFound phrase contains arguments.", _socialDefinition.Name);
                        systemContext.Msg.To(actor).Send("They aren't here.");
                    }
                    useCharacterNotFound = true;
                }
            }
        }

        //
        if (useCharacterNotFound)
            systemContext.Msg.To(actor).Act(_socialDefinition.CharacterNotFound!).With(actor);
        else if (victim == default)
        {
            if (_socialDefinition.CharacterNoArg is not null)
               systemContext.Msg.To(actor).Act(_socialDefinition.CharacterNoArg).With(actor);
            if (_socialDefinition.OthersNoArg is not null)
                systemContext.Msg.ToRoom(actor).Act(_socialDefinition.OthersNoArg).With(actor);
        }
        else if (victim == actor)
        {
            if (_socialDefinition.CharacterAuto is not null)
               systemContext.Msg.To(actor).Act(_socialDefinition.CharacterAuto).With(actor);
            if (_socialDefinition.OthersAuto is not null)
                systemContext.Msg.ToRoom(actor).Act(_socialDefinition.OthersAuto).With(actor);
        }
        else
        {
            if (_socialDefinition.CharacterFound is not null)
               systemContext.Msg.To(actor).Act(_socialDefinition.CharacterFound).With(actor, victim);
            if (_socialDefinition.VictimFound is not null)
                systemContext.Msg.To(victim).Act(_socialDefinition.VictimFound).With(actor, victim);
            if (_socialDefinition.OthersFound is not null)
                systemContext.Msg.ToRoomExcept(actor, victim).Act(_socialDefinition.OthersFound).With(actor, victim);

            // TODO
            // trigger mob program
            //(Victim as INonPlayableCharacter)?.OnSocial(Actor, SocialDefinition);
        }
    }
}
