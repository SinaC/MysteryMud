using MysteryMud.Core.Contracts;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Tests.Infrastructure;

internal class TestIntentContainer : IIntentContainer
{
    public readonly TestIntentBuffer<ActionIntent> Action = new();
    public readonly TestIntentBuffer<FleeIntent> Flee = new();
    public readonly TestIntentBuffer<MoveIntent> Move = new();
    public readonly TestIntentBuffer<GetItemIntent> GetItem = new();
    public readonly TestIntentBuffer<DropItemIntent> DropItem = new();
    public readonly TestIntentBuffer<GiveItemIntent> GiveItem = new();
    public readonly TestIntentBuffer<PutItemIntent> PutItem = new();
    public readonly TestIntentBuffer<WearItemIntent> WearItem = new();
    public readonly TestIntentBuffer<RemoveItemIntent> RemoveItem = new();
    public readonly TestIntentBuffer<DestroyItemIntent> DestroyItem = new();
    public readonly TestIntentBuffer<SacrificeItemIntent> SacrificeItem = new();
    public readonly TestIntentBuffer<UseAbilityIntent> UseAbility = new();
    public readonly TestIntentBuffer<ExecuteAbilityIntent> ExecuteAbility = new();
    public readonly TestIntentBuffer<CorpseLootIntent> CorpseLoot = new();
    public readonly TestIntentBuffer<LookIntent> Look = new();
    public readonly TestIntentBuffer<ScheduleIntent> Schedule = new();

    // IIntentWriterContainer
    IIntentWriter<ActionIntent> IIntentWriterContainer.Action => Action;
    IIntentWriter<FleeIntent> IIntentWriterContainer.Flee => Flee;
    IIntentWriter<MoveIntent> IIntentWriterContainer.Move => Move;
    IIntentWriter<GetItemIntent> IIntentWriterContainer.GetItem => GetItem;
    IIntentWriter<DropItemIntent> IIntentWriterContainer.DropItem => DropItem;
    IIntentWriter<GiveItemIntent> IIntentWriterContainer.GiveItem => GiveItem;
    IIntentWriter<PutItemIntent> IIntentWriterContainer.PutItem => PutItem;
    IIntentWriter<WearItemIntent> IIntentWriterContainer.WearItem => WearItem;
    IIntentWriter<RemoveItemIntent> IIntentWriterContainer.RemoveItem => RemoveItem;
    IIntentWriter<DestroyItemIntent> IIntentWriterContainer.DestroyItem => DestroyItem;
    IIntentWriter<SacrificeItemIntent> IIntentWriterContainer.SacrificeItem => SacrificeItem;
    IIntentWriter<UseAbilityIntent> IIntentWriterContainer.UseAbility => UseAbility;
    IIntentWriter<ExecuteAbilityIntent> IIntentWriterContainer.ExecuteAbility => ExecuteAbility;
    IIntentWriter<CorpseLootIntent> IIntentWriterContainer.CorpseLoot => CorpseLoot;
    IIntentWriter<LookIntent> IIntentWriterContainer.Look => Look;
    IIntentWriter<ScheduleIntent> IIntentWriterContainer.Schedule => Schedule;

    // IIntentContainer
    public ActionIntent ActionByIndex(int index) => Action.ByIndex(index);
    public int ActionCount => Action.Count;
    public Span<FleeIntent> FleeSpan => Flee.Span;
    public Span<MoveIntent> MoveSpan => Move.Span;
    public Span<GetItemIntent> GetItemSpan => GetItem.Span;
    public Span<DropItemIntent> DropItemSpan => DropItem.Span;
    public Span<GiveItemIntent> GiveItemSpan => GiveItem.Span;
    public Span<PutItemIntent> PutItemSpan => PutItem.Span;
    public Span<WearItemIntent> WearItemSpan => WearItem.Span;
    public Span<RemoveItemIntent> RemoveItemSpan => RemoveItem.Span;
    public Span<DestroyItemIntent> DestroyItemSpan => DestroyItem.Span;
    public Span<SacrificeItemIntent> SacrificeItemSpan => SacrificeItem.Span;
    public Span<UseAbilityIntent> UseAbilitySpan => UseAbility.Span;
    public Span<ExecuteAbilityIntent> ExecuteAbilitySpan => ExecuteAbility.Span;
    public Span<CorpseLootIntent> CorpseLootSpan => CorpseLoot.Span;
    public Span<LookIntent> LookSpan => Look.Span;
    public Span<ScheduleIntent> ScheduleSpan => Schedule.Span;

    public void ClearAll()
    {
        Action.Clear(); Flee.Clear(); Move.Clear();
        GetItem.Clear(); DropItem.Clear(); GiveItem.Clear();
        PutItem.Clear(); WearItem.Clear(); RemoveItem.Clear();
        DestroyItem.Clear(); SacrificeItem.Clear();
        UseAbility.Clear(); ExecuteAbility.Clear();
        CorpseLoot.Clear(); Look.Clear(); Schedule.Clear();
    }
}