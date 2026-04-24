using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using System.Diagnostics;
using System.Text;
using TinyECS;

namespace MysteryMud.Domain.Formatters;

// IFormattable cannot be used because formatting depends on who'll receive the message (CanSee check)
public static class ActFormatter
{
    public static string FormatActOneLine(World world, EntityId target, string format, params object[]? arguments)
    {
        StringBuilder result = new();

        var state = ActParsingStates.Normal;
        var argumentIndex = 0;
        object? currentArgument = null!;
        StringBuilder argumentFormat = null!;
        foreach (char c in format)
        {
            switch (state)
            {
                case ActParsingStates.Normal: // searching for {
                    if (c == '{')
                    {
                        state = ActParsingStates.OpeningBracketFound;
                        currentArgument = null!;
                        argumentFormat = new StringBuilder();
                    }
                    else
                        result.Append(c);
                    break;
                case ActParsingStates.OpeningBracketFound: // searching for a number
                    if (c == '{') // {{ -> {
                    {
                        result.Append('{');
                        state = ActParsingStates.Normal;
                    }
                    else if (c == '}') // {} -> nothing
                    {
                        state = ActParsingStates.Normal;
                    }
                    else if (c >= '0' && c <= '9') // {x -> argument found
                    {
                        argumentIndex = c - '0';
                        if (argumentIndex >= arguments?.Length)
                        {
                            // TODO: better error handling, including logging the error with format and target information
                            //Logger.LogError("FormatActOneLine for format {format} and target {target} tries to access argument {argumentIndex} but {argumentCount} were provided.", format, target.DebugName, argumentIndex, arguments.Length);
                            return "%R%!!Error!!%x%";
                        }
                        currentArgument = arguments?[argumentIndex];
                        state = ActParsingStates.ArgumentFound;
                    }
                    break;
                case ActParsingStates.ArgumentFound: // searching for } or :
                    if (c == '}')
                    {
                        FormatActOneArgument(world, target, result, argumentIndex, null, currentArgument);
                        state = ActParsingStates.Normal;
                    }
                    else if (c == ':')
                        state = ActParsingStates.FormatSeparatorFound;
                    break;
                case ActParsingStates.FormatSeparatorFound: // searching for }
                    if (c == '}')
                    {
                        Debug.Assert(argumentFormat != null);
                        FormatActOneArgument(world, target, result, argumentIndex, argumentFormat.ToString(), currentArgument);
                        state = ActParsingStates.Normal;
                    }
                    else
                    {
                        // argumentFormat cannot be null
                        Debug.Assert(argumentFormat != null);
                        argumentFormat.Append(c);
                    }
                    break;
            }
        }
        if (result.Length > 0)
            result[0] = char.ToUpperInvariant(result[0]);
        return result.ToString();
    }

    private enum ActParsingStates
    {
        Normal,
        OpeningBracketFound,
        ArgumentFound,
        FormatSeparatorFound,
    }

    // Formatting
    //  Character
    //      default: same as n, N
    //      p, P: same as n but you is replaced with your
    //      n, N: you/argument.name if visible by target, someone otherwise
    //      e, E: you/he/she/it, depending on argument.sex
    //      m, M: you/him/her/it, depending on argument.sex
    //      s, S: your/his/her/its, depending on argument.sex
    //      f, F: yourself/himself/herself/itself
    //      y, Y: your/name's
    //      b, B: are/is
    //      h, H: have/has
    //      v, V: add 's' at the end of a verb if argument is different than target (take care of verb ending with y/o/h
    // Item
    //      argument.Name if visible by target, something otherwise
    // TODO Exit
    //      exit name
    // TODO Ability
    //      ability name
    private static void FormatActOneArgument(World world, EntityId target, StringBuilder sb, int argumentIndex, ReadOnlySpan<char> argumentFormat, object? argument)
    {
        if (argument is EntityId entity && !world.IsAlive(entity))
            return;

        if (argument is EntityId character && world.Has<CharacterTag>(character))
        {
            var letter = argumentFormat.Length > 0 ? argumentFormat[0] : (argumentIndex == 0 ? 'N' : 'n'); // default to 'N' or 'n' if no format specified
            ref var gender = ref world.TryGetRef<Gender>(character, out var hasGender);
            var genderKind = hasGender ? gender.Value : GenderKind.Neutral;
            switch (letter)
            {
                case 'p':
                    if (target == character) sb.Append("your");
                    else AppendPossessive(sb, RelativeDisplayName(world, character, target));
                    break;
                case 'P':
                    if (target == character) sb.Append("Your");
                    else AppendPossessive(sb, RelativeDisplayName(world, character, target), true);
                    break;
                case 'n':
                    sb.Append(target == character ? "you" : RelativeDisplayName(world, character, target));
                    break;
                case 'N':
                    sb.Append(target == character ? "You" : RelativeDisplayName(world, character, target).FirstCharToUpper());
                    break;
                case 'e':
                    sb.Append(target == character ? "you" : genderKind.Subject);
                    break;
                case 'E':
                    sb.Append(target == character ? "You" : genderKind.Subject.FirstCharToUpper());
                    break;
                case 'm':
                    sb.Append(target == character ? "you" : genderKind.Objective);
                    break;
                case 'M':
                    sb.Append(target == character ? "You" : genderKind.Objective.FirstCharToUpper());
                    break;
                case 's':
                    sb.Append(target == character ? "your" : genderKind.Possessive);
                    break;
                case 'S':
                    sb.Append(target == character ? "Your" : genderKind.Possessive.FirstCharToUpper());
                    break;
                case 'f':
                    sb.Append(target == character ? "your" : genderKind.Objective);
                    sb.Append("self");
                    break;
                case 'F':
                    sb.Append(target == character ? "your" : genderKind.Objective.FirstCharToUpper());
                    sb.Append("self");
                    break;
                case 'y':
                    if (target == character) sb.Append("your");
                    else sb.Append(RelativeDisplayName(world, character, target)).Append("'s");
                    break;
                case 'Y':
                    if (target == character) sb.Append("Your");
                    else sb.Append(RelativeDisplayName(world, character, target)).Append("'s");
                    break;
                case 'b':
                    sb.Append(target == character ? "are" : "is");
                    break;
                case 'B':
                    sb.Append(target == character ? "Are" : "Is");
                    break;
                case 'h':
                    sb.Append(target == character ? "have" : "has");
                    break;
                case 'H':
                    sb.Append(target == character ? "Have" : "Has");
                    break;
                case 'v':
                case 'V':
                    if (target != character) AppendVerbSuffix(sb);
                    break;
                default:
                    sb.Append("<???>");
                    break;
            }
            return;
        }

        if (argument is EntityId item && world.Has<ItemTag>(item))
        {
            var letter = argumentFormat.Length > 0 ? argumentFormat[0] : (argumentIndex == 0 ? 'N' : 'n'); // default to 'N' or 'n' if no format specified
            var genderKind = GenderKind.Neutral;
            switch (letter)
            {
                case 'p':
                    AppendPossessive(sb, RelativeDisplayName(world, item, target));
                    break;
                case 'P':
                    AppendPossessive(sb, RelativeDisplayName(world, item, target), true);
                    break;
                case 'n':
                    sb.Append(RelativeDisplayName(world, item, target));
                    break;
                case 'N':
                    sb.Append(RelativeDisplayName(world, item, target).FirstCharToUpper());
                    break;
                case 'e':
                    sb.Append(genderKind.Subject);
                    break;
                case 'E':
                    sb.Append(genderKind.Subject.FirstCharToUpper());
                    break;
                case 'm':
                    sb.Append(genderKind.Objective);
                    break;
                case 'M':
                    sb.Append(genderKind.Objective.FirstCharToUpper());
                    break;
                case 's':
                    sb.Append(genderKind.Possessive);
                    break;
                case 'S':
                    sb.Append(genderKind.Possessive.FirstCharToUpper());
                    break;
                case 'f':
                    sb.Append(genderKind.Objective);
                    sb.Append("self");
                    break;
                case 'F':
                    sb.Append(genderKind.Objective.FirstCharToUpper());
                    sb.Append("self");
                    break;
                case 'y':
                    sb.Append(RelativeDisplayName(world, item, target)).Append("'s");
                    break;
                case 'Y':
                    sb.Append(RelativeDisplayName(world, item, target)).Append("'s");
                    break;
                case 'b':
                    sb.Append("is");
                    break;
                case 'B':
                    sb.Append("Is");
                    break;
                case 'h':
                    sb.Append("has");
                    break;
                case 'H':
                    sb.Append("Has");
                    break;
                case 'v':
                case 'V':
                    AppendVerbSuffix(sb);
                    break;
                default:
                    sb.Append("<???>");
                    break;
            }
            return;
        }

        if (argument is EntityId room && world.Has<Room>(room))
        {
            sb.Append(EntityHelpers.DisplayName(world, room));
            return;
        }

        // TODO
        //if (argument is IExit exit)
        //{
        //    sb.Append(exit.Keywords.FirstOrDefault() ?? "door");
        //    return;
        //}

        //if (argument is IAbilityLearned abilityLearned)
        //{
        //    sb.Append(abilityLearned.Name);
        //    return;
        //}

        if (argument is null)
        {
            sb.Append("nothing");
            return;
        }

        if (argumentFormat.IsEmpty)
            sb.Append(argument);
        else if (argument is IFormattable formattable)
            sb.Append(formattable.ToString(argumentFormat.ToString(), null));
        else
            sb.Append(argument);
    }

    private static string RelativeDisplayName(World world, EntityId entity, EntityId observer)
    {
        // TODO
        // Placeholder: in a real implementation, this would check visibility, relationships, etc.
        return EntityHelpers.DisplayName(world, entity);
    }

    // Helper to append possessive without extra string allocation
    private static void AppendPossessive(StringBuilder sb, string name, bool upperFirst = false)
    {
        if (upperFirst && name.Length > 0)
        {
            sb.Append(char.ToUpperInvariant(name[0]));
            sb.Append(name.AsSpan(1));
        }
        else
            sb.Append(name);

        if (sb[^1] == 's')
            sb.Append('\'');
        else
            sb.Append("'s");
    }

    // Helper for simple verb suffix rules
    private static void AppendVerbSuffix(StringBuilder sb)
    {
        if (sb.Length == 0)
            return;

        char last = sb[^1];
        char beforeLast = sb.Length > 1 ? sb[^2] : ' ';

        if (last == 'y' && !"aeiou ".Contains(beforeLast))
        {
            sb.Length--;
            sb.Append("ies");
        }
        else if ("ohsx".Contains(last))
            sb.Append("es");
        else
            sb.Append('s');
    }
}
