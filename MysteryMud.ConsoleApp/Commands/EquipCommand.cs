using Arch.Core;

namespace MysteryMud.ConsoleApp.Commands;

record EquipCommand(Entity Actor, string ItemName) : ICommand;
