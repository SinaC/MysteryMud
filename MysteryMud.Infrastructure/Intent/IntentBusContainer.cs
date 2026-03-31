using MysteryMud.Core.Intent;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Infrastructure.Intent;

public sealed class IntentBusContainer : IIntentContainer
{
    // FleeSystem
    private readonly StructBuffer<FleeIntent> _flee = new(128);
    // MoveSystem
    private readonly StructBuffer<MoveIntent> _move = new(256);
    // ItemInteractionSystem
    private readonly StructBuffer<GetItemIntent> _getItem = new(128);
    private readonly StructBuffer<DropItemIntent> _dropItem = new(128);
    private readonly StructBuffer<GiveItemIntent> _giveItem = new(128);
    private readonly StructBuffer<PutItemIntent> _putItem = new(128);
    private readonly StructBuffer<WearItemIntent> _wearItem = new(128);
    private readonly StructBuffer<RemoveItemIntent> _removeItem = new(128);
    private readonly StructBuffer<DestroyItemIntent> _destroyItem = new(128);
    private readonly StructBuffer<SacrificeItemIntent> _sacrificeItem = new(128);
    // CombatSystem
    private readonly StructBuffer<AttackIntent> _attack = new(1024); // 2 buffers for double buffering
    // LootSystem
    private readonly StructBuffer<LootIntent> _loot = new(128);
    // LookSystem
    private readonly StructBuffer<LookIntent> _look = new(128);
    // ScheduleSystem
    private readonly StructBuffer<ScheduleIntent> _schedule = new(512);

    public IntentBusContainer()
    {
        // FleeSystem
        Flee = new IntentWriter<FleeIntent>(_flee);
        // MoveSystem
        Move = new IntentWriter<MoveIntent>(_move);
        // ItemInteractionSystem
        GetItem = new IntentWriter<GetItemIntent>(_getItem);
        DropItem = new IntentWriter<DropItemIntent>(_dropItem);
        GiveItem = new IntentWriter<GiveItemIntent>(_giveItem);
        PutItem = new IntentWriter<PutItemIntent>(_putItem);
        WearItem = new IntentWriter<WearItemIntent>(_wearItem);
        RemoveItem = new IntentWriter<RemoveItemIntent>(_removeItem);
        DestroyItem = new IntentWriter<DestroyItemIntent>(_destroyItem);
        SacrificeItem = new IntentWriter<SacrificeItemIntent>(_sacrificeItem);
        // CombatSystem
        Attack = new IntentWriter<AttackIntent>(_attack);
        // LootSystem
        Loot = new IntentWriter<LootIntent>(_loot);
        // LookSystem
        Look = new IntentWriter<LookIntent>(_look);
        // ScheduleSystem
        Schedule = new IntentWriter<ScheduleIntent>(_schedule);
    }

    // Attack intents are a special case, we want to able to have direct access, because CombatOrchestrator add attack intents while iterating them
    public AttackIntent AttackByIndex(int index) => _attack[index];
    public int AttackCount => _attack.Count;

    // FleeSystem
    public IIntentWriter<FleeIntent> Flee { get; }
    public Span<FleeIntent> FleeSpan => _flee.AsSpan();
    // MoveSystem
    public IIntentWriter<MoveIntent> Move { get; }
    public Span<MoveIntent> MoveSpan => _move.AsSpan();
    // ItemInteractionSystem
    public IIntentWriter<GetItemIntent> GetItem { get; }
    public Span<GetItemIntent> GetItemSpan => _getItem.AsSpan();
    public IIntentWriter<DropItemIntent> DropItem { get; }
    public Span<DropItemIntent> DropItemSpan => _dropItem.AsSpan();
    public IIntentWriter<GiveItemIntent> GiveItem { get; }
    public Span<GiveItemIntent> GiveItemSpan => _giveItem.AsSpan();
    public IIntentWriter<PutItemIntent> PutItem { get; }
    public Span<PutItemIntent> PutItemSpan => _putItem.AsSpan();
    public IIntentWriter<WearItemIntent> WearItem { get; }
    public Span<WearItemIntent> WearItemSpan => _wearItem.AsSpan();
    public IIntentWriter<RemoveItemIntent> RemoveItem { get; }
    public Span<RemoveItemIntent> RemoveItemSpan => _removeItem.AsSpan();
    public IIntentWriter<DestroyItemIntent> DestroyItem { get; }
    public Span<DestroyItemIntent> DestroyItemSpan => _destroyItem.AsSpan();
    public IIntentWriter<SacrificeItemIntent> SacrificeItem { get; }
    public Span<SacrificeItemIntent> SacrificeItemSpan => _sacrificeItem.AsSpan();
    // CombatSystem
    public IIntentWriter<AttackIntent> Attack { get; }
    public Span<AttackIntent> AttackSpan => _attack.AsSpan();
    // LootSystem
    public IIntentWriter<LootIntent> Loot { get; }
    public Span<LootIntent> LootSpan => _loot.AsSpan();
    // LootSystem
    public IIntentWriter<LookIntent> Look { get; }
    public Span<LookIntent> LookSpan => _look.AsSpan();
    // ScheduleSystem
    public IIntentWriter<ScheduleIntent> Schedule { get; }
    public Span<ScheduleIntent> ScheduleSpan => _schedule.AsSpan();

    public void ClearAll()
    {
        // FleeSystem
        _flee.Clear();
        // MoveSystem
        _move.Clear();
        // ItemInteractionSystem
        _getItem.Clear();
        _dropItem.Clear();
        _giveItem.Clear();
        _putItem.Clear();
        _wearItem.Clear();
        _removeItem.Clear();
        _destroyItem.Clear();
        _sacrificeItem.Clear();
        // CombatSystem
        _attack.Clear();
        // LootSystem
        _loot.Clear();
        // LookSystem
        _look.Clear();
        // ScheduleSystem
        _schedule.Clear();
    }
}