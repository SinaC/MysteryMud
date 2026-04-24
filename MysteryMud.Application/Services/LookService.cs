using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Services;

public class LookService : ILookService
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public LookService(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void DescribeRoom(EntityId viewer, EntityId room)
    {
        // Get room name, description and contents and graph
        ref var roomName = ref _world.Get<Name>(room);
        ref var roomDescription = ref _world.Get<Description>(room);
        ref var roomContents = ref _world.Get<RoomContents>(room);
        ref var roomGraph = ref _world.Get<RoomGraph>(room);
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        _msg.To(viewer).Send($"{roomName.Value}");
        _msg.To(viewer).Send($"{roomDescription.Value}");
        if (!roomGraph.Exits.HasAnyExit())
        {
            _msg.To(viewer).Send("No exits.");
        }
        else
        {
            _msg.To(viewer).Send("Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                if (exit is null)
                    continue;
                _msg.To(viewer).Send($"- {exit!.Value.Direction} - {EntityHelpers.DisplayName(_world, exit!.Value.TargetRoom)}");
            }
        }
        _msg.To(viewer).Send("Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(viewer)) continue; // skip self
            _msg.To(viewer).Send($"- {EntityHelpers.DisplayName(_world, c)}");
        }

        _msg.To(viewer).Send("Items here:");
        foreach (var item in roomItems)
        {
            _msg.To(viewer).Send($"- {EntityHelpers.DisplayName(_world, item)}");
        }
    }

    public void DescribeCharacter(EntityId viewer, EntityId target)
    {
        _msg.To(viewer).Send($"{EntityHelpers.DisplayName(_world, target)}");

        ref var targetEquipment = ref _world.TryGetRef<Equipment>(target, out var hasEquipment);
        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (targetEquipment.Slots.TryGetValue(slot, out var item))
                _msg.To(viewer).Send($"{slot}: {EntityHelpers.DisplayName(_world, item)}");
        }

        // only if peek skill is high enough
        ref var targetInventory = ref _world.TryGetRef<Inventory>(target, out var hasTargetInventory);
        if (hasTargetInventory)
        {
            foreach (var item in targetInventory.Items)
            {
                _msg.To(viewer).Act("{0} {0:b} carrying: {1}").With(target, item);
            }
        }
    }

    public void DescribeItem(EntityId viewer, EntityId item)
    {
        _msg.To(viewer).Send($"{EntityHelpers.DisplayName(_world, item)}");

        // TODO: remove, should be in ExamineCommand
        ref var containerContents = ref _world.TryGetRef<ContainerContents>(item, out var isContainerContents);
        if (isContainerContents)
        {
            foreach (var containerItem in containerContents.Items)
            {
                _msg.To(viewer).Send($"It contains: {EntityHelpers.DisplayName(_world, containerItem)}");
            }
        }
    }
}
