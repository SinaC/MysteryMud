using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Services;

public class LookService : ILookService
{
    private readonly IGameMessageService _msg;

    public LookService(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void DescribeRoom(Entity viewer, Entity room)
    {
        // Get room name, description and contents and graph
        ref var roomName = ref room.Get<Name>();
        ref var roomDescription = ref room.Get<Description>();
        ref var roomContents = ref room.Get<RoomContents>();
        ref var roomGraph = ref room.Get<RoomGraph>();
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
                _msg.To(viewer).Send($"- {exit!.Value.Direction} - {exit!.Value.TargetRoom.DisplayName}");
            }
        }
        _msg.To(viewer).Send("Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(viewer)) continue; // skip self
            _msg.To(viewer).Send($"- {c.DisplayName}");
        }

        _msg.To(viewer).Send("Items here:");
        foreach (var item in roomItems)
        {
            _msg.To(viewer).Send($"- {item.DisplayName}");
        }
    }

    public void DescribeCharacter(Entity viewer, Entity target)
    {
        _msg.To(viewer).Send($"{target.DisplayName}");

        ref var targetEquipment = ref target.TryGetRef<Equipment>(out var hasEquipment);
        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (targetEquipment.Slots.TryGetValue(slot, out var item))
                _msg.To(viewer).Send($"{slot}: {item.DisplayName}");
        }

        // only if peek skill is high enough
        ref var targetInventory = ref target.TryGetRef<Inventory>(out var hasTargetInventory);
        if (hasTargetInventory)
        {
            foreach (var item in targetInventory.Items)
            {
                _msg.To(viewer).Act("{0} {0:b} carrying: {1}").With(target, item);
            }
        }
    }

    public void DescribeItem(Entity viewer, Entity item)
    {
        _msg.To(viewer).Send($"{item.DisplayName}");

        // TODO: remove, should be in ExamineCommand
        ref var containerContents = ref item.TryGetRef<ContainerContents>(out var isContainerContents);
        if (isContainerContents)
        {
            foreach (var containerItem in containerContents.Items)
            {
                _msg.To(viewer).Send($"It contains: {containerItem.DisplayName}");
            }
        }
    }
}
