using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Intent;

public interface IIntentContainer: IIntentWriterContainer
{
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
    Span<AttackIntent> AttackSpan { get; } // can only iterate on the current attack buffer, not the next buffer
    // LootSystem
    Span<LootIntent> LootSpan { get; }
    // LookSystem
    Span<LookIntent> LookSpan { get; }

    void ClearAll();
}
