using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Intent;

public interface IIntentContainer: IIntentWriterContainer
{
    // Attack intents are a special case, we want to able to have direct access, because CombatOrchestrator add attack intents while iterating them
    AttackIntent AttackByIndex(int index);
    int AttackCount { get; }

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
    // CombatSystem
    Span<AttackIntent> AttackSpan { get; }
    // LootSystem
    Span <LootIntent> LootSpan { get; }
    // LookSystem
    Span<LookIntent> LookSpan { get; }
    // ScheduleSystem
    Span<ScheduleIntent> ScheduleSpan { get; }

    void ClearAll();
}
