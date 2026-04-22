using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class PeaceCommand : ICommand
{
    private readonly IGameMessageService _msg;

    public PeaceCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters.ToArray();
        foreach (var character in people)
        {
            CombatHelpers.RemoveFromCombat(state, character);
        }
        _msg.To(actor).Send("Ok.");
    }
    //public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    //{
    //    ref var people = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;

    //    Console.WriteLine($"Before loop: [{string.Join(", ", people.Select(e => e.DebugName))}]");
    //    foreach (var character in actor.Get<Location>().Room.Get<RoomContents>().Characters)
    //        Console.WriteLine($"Entity: Id={character.Id} Version={character.Version}");
    //    Console.WriteLine($"List reference hashcode: {people.GetHashCode()}");

    //    var snapshot = people.ToArray();
    //    foreach (var character in snapshot)
    //    {
    //        Console.WriteLine($"Processing: {character.DebugName}, Has<CombatState>: {character.Has<CombatState>()}");

    //        var countBefore = people.Count;
    //        Console.WriteLine($"Before: Id={character.Id} Version={character.Version}");
    //        //if (character.Has<CombatState>())
    //            character.Remove<CombatState>();
    //        Console.WriteLine($"After: Id={character.Id} Version={character.Version}");
    //        var countAfter = people.Count;
    //        // re-fetch the entity from the world to see if version changed

    //        Console.WriteLine($"After Remove<CombatState>: [{string.Join(", ", people.Select(e => e.DebugName))}]");
    //        Console.WriteLine($"Count before: {countBefore}, after: {countAfter}");

    //        // check if the list reference itself changed
    //        ref var peopleAfter = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
    //        Console.WriteLine($"Same list reference: {ReferenceEquals(people, peopleAfter)}");
    //        // also check what the world thinks is at this Id now
    //    }

    //    Console.WriteLine($"After loop: [{string.Join(", ", people.Select(e => e.DebugName))}]");
    //    foreach (var character in actor.Get<Location>().Room.Get<RoomContents>().Characters)
    //        Console.WriteLine($"Entity: Id={character.Id} Version={character.Version}");
    //}
}
