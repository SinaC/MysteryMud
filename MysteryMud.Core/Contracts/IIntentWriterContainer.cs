using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Contracts;

public interface IIntentWriterContainer
{
    // ActionOrchestrator
    IIntentWriter<ActionIntent> Action { get; }

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
    // AbilitySystem
    IIntentWriter<UseAbilityIntent> UseAbility { get; }
    IIntentWriter<ExecuteAbilityIntent> ExecuteAbility { get; }
    // LootSystem
    IIntentWriter<CorpseLootIntent> CorpseLoot { get; }
    // LookSystem
    IIntentWriter<LookIntent> Look { get; }
    // ScheduleSystem
    IIntentWriter<ScheduleIntent> Schedule { get; }
}
