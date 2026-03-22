namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public enum ArgScope
{
    Inventory,
    Room,
    InventoryThenRoom,
    RoomThenInventory,
    ContainerOnly // only if container is specified
}
