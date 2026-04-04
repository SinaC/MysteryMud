using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Intent;

public interface IIntentWriterContainer
{
    // AttackOrchestrator
    IIntentWriter<AttackIntent> Attack { get; }
    // EffectOrchestrator (player/AI casting spells, scripted triggers, delayed triggers from scheduler)
    IIntentWriter<EffectIntent> Effect { get; }

    // FleeSystem
    IIntentWriter<FleeIntent> Flee { get; }
    // MoveSystem
    IIntentWriter<MoveIntent> Move { get; }
    // ItemInteractionSystem
    IIntentWriter<GetItemIntent> GetItem { get; }
    IIntentWriter<DropItemIntent> DropItem { get; }
    IIntentWriter<GiveItemIntent> GiveItem { get; }
    IIntentWriter<PutItemIntent> PutItem { get; }
    IIntentWriter<WearItemIntent> WearItem { get; }
    IIntentWriter<RemoveItemIntent> RemoveItem { get; }
    IIntentWriter<DestroyItemIntent> DestroyItem { get; }
    IIntentWriter<SacrificeItemIntent> SacrificeItem { get; }
    // LootSystem
    IIntentWriter<LootIntent> Loot { get; }
    // LookSystem
    IIntentWriter<LookIntent> Look { get; }
    // ScheduleSystem
    IIntentWriter<ScheduleIntent> Schedule { get; }
}
