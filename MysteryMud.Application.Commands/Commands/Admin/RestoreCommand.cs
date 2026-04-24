using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class RestoreCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public RestoreCommand(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        // TODO: character, all

        ref var room = ref _world.Get<Location>(actor).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        foreach (var character in people)
        {
            SetResourceToMax<Health>(character, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Move>(character, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Mana>(character, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Energy>(character, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Rage>(character, x => x.Max, (ref x, value) => x.Current = value);

            _msg.To(character).Act("{0} {0:h} restored you.").With(actor);
        }

        _msg.To(actor).Send("Ok.");
    }

    private delegate void ResourceValueSetter<TResource>(ref TResource resource, int value);

    private void SetResourceToMax<TResource>(EntityId character, Func<TResource, int> getMaxValueFunc, ResourceValueSetter<TResource> setResourceValueAction)
       where TResource : struct
    {
        ref var resource = ref _world.TryGetRef<TResource>(character, out var hasResource);
        if (!hasResource)
            return;

        var maxResource = getMaxValueFunc(resource);
        setResourceValueAction(ref resource, maxResource);
    }
}
