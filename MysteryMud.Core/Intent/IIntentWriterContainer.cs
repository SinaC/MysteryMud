using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Intent;

public interface IIntentWriterContainer
{
    // FleeSystem
    IIntentWriter<FleeIntent> Flee { get; }
    // MoveSystem
    IIntentWriter<MoveIntent> Move { get; }
    // InteractionSystem
    IIntentWriter<GetItemIntent> GetItem { get; }
    IIntentWriter<DropItemIntent> DropItem { get; }
    IIntentWriter<GiveItemIntent> GiveItem { get; }
    IIntentWriter<PutItemIntent> PutItem { get; }
    // CombatSystem
    IIntentWriter<AttackIntent> Attack { get; } // can write to current attack buffer
    IIntentWriter<AttackIntent> AttackNext { get; } // can write to next attack buffer, for counterattacks and multiattack combos
    // LootSystem
    IIntentWriter<LootIntent> Loot { get; }
}
