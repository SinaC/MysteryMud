using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class ThreatCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly EntitySet _hasThreatTableEntitySet;

    public ThreatCommand(World world, IGameMessageService msg)
    {
        _msg = msg;
        _hasThreatTableEntitySet = world
            .GetEntities()
            .With<ThreatTable>()
            .With<ActiveThreatTag>()
            .AsSet();
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        foreach(var entity in _hasThreatTableEntitySet.GetEntities())
        {
            ref var threatTable = ref entity.Get<ThreatTable>();
            _msg.To(actor).Send($"Entity {entity.DebugName}:");
            foreach(var entry in threatTable.Entries)
            {
                _msg.To(actor).Send($"  Target Entity: {entry.Key.DebugName}, Threat Value: {entry.Value}");
            }
        }
    }
}
