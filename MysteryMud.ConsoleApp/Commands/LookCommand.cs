using Arch.Core;

namespace MysteryMud.ConsoleApp.Commands;

record LookCommand(Entity Actor) : ICommand;
