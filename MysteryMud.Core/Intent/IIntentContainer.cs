using MysteryMud.GameData.Intents;

namespace MysteryMud.Core.Intent;

public interface IIntentContainer: IIntentWriterContainer
{
    // FleeSystem
    Span<FleeIntent> FleeSpan { get; }
    // MoveSystem
    Span<MoveIntent> MoveSpan { get; }
    // InteractionSystem
    Span<GetItemIntent> GetItemSpan { get; }
    Span<DropItemIntent> DropItemSpan { get; }
    Span<GiveItemIntent> GiveItemSpan { get; }
    Span<PutItemIntent> PutItemSpan { get; }
    // CombatSystem
    Span<AttackIntent> AttackSpan { get; } // can only iterate on the current attack buffer, not the next buffer
    // LootSystem
    Span<LootIntent> LootSpan { get; }

    // Swap attack and attack next buffers
    void SwapAttackBuffers();

    void ClearAll();
}
