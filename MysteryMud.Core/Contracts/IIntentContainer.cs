using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Contracts;

public interface IIntentContainer : IIntentWriterContainer
{
    // Action intents are a special case, we want to able to have direct access, because ActionOrchestrator add attack intents while iterating them
    ActionIntent ActionByIndex(int index);
    int ActionCount { get; }

    // FleeSystem
    Span<FleeIntent> FleeSpan { get; }
    // MoveSystem
    Span<MoveIntent> MoveSpan { get; }
    // ItemInteractionSystem
    Span<GetItemIntent> GetItemSpan { get; }
    Span<DropItemIntent> DropItemSpan { get; }
    Span<GiveItemIntent> GiveItemSpan { get; }
    Span<PutItemIntent> PutItemSpan { get; }
    Span<WearItemIntent> WearItemSpan { get; }
    Span<RemoveItemIntent> RemoveItemSpan { get; }
    Span<DestroyItemIntent> DestroyItemSpan { get; }
    Span<SacrificeItemIntent> SacrificeItemSpan { get; }
    // AbilitySystem
    Span<UseAbilityIntent> UseAbilitySpan { get; }
    Span<ExecuteAbilityIntent> ExecuteAbilitySpan { get; }
    // LootSystem
    Span<CorpseLootIntent> CorpseLootSpan { get; }
    // LookSystem
    Span<LookIntent> LookSpan { get; }
    // ScheduleSystem
    Span<ScheduleIntent> ScheduleSpan { get; }

    void ClearAll();
}
