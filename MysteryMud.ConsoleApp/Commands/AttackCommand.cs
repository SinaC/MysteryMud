using Arch.Core;

namespace MysteryMud.ConsoleApp.Commands;

record AttackCommand(Entity Attacker, Entity Target) : ICommand;
