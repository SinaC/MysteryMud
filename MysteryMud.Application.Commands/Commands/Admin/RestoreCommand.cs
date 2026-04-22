using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class RestoreCommand : ICommand
{
    private readonly IGameMessageService _msg;

    public RestoreCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        // TODO: character, all

        ref var location = ref actor.Get<Location>();
        ref var roomContents = ref location.Room.Get<RoomContents>();
        foreach (var character in roomContents.Characters)
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

    private void SetResourceToMax<TResource>(Entity player, Func<TResource, int> getMaxValueFunc, ResourceValueSetter<TResource> setResourceValueAction)
       where TResource : struct
    {
        ref var resource = ref player.TryGetRef<TResource>(out var hasResource);
        if (!hasResource)
            return;

        var maxResource = getMaxValueFunc(resource);
        setResourceValueAction(ref resource, maxResource);
    }
}
