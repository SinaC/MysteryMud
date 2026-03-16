using Arch.Core;

namespace MysteryMud.ConsoleApp.Events;

record AttackEvent(Entity Attacker, Entity Target, int Damage);
