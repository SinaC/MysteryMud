using MysteryMud.Core.Contracts;
using MysteryMud.GameData.Intents;
using MysteryMud.Infrastructure.Buffers;

namespace MysteryMud.Infrastructure.Intent;

public sealed class IntentBusContainer : IIntentContainer
{
    // ActionOrchestrator
    private readonly StructBuffer<ActionIntent> _action = new(1024);

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
    // AbilitySystem
    private readonly StructBuffer<UseAbilityIntent> _useAbility = new(256);
    private readonly StructBuffer<ExecuteAbilityIntent> _executeAbility = new(256);
    // LootSystem
    private readonly StructBuffer<CorpseLootIntent> _corpseLoot = new(128);
    // AutoSacrificeSystem
    private readonly StructBuffer<AutoSacrificeIntent> _autoSacrifice = new(128);
    // LookSystem
    private readonly StructBuffer<LookIntent> _look = new(128);
    // ScheduleSystem
    private readonly StructBuffer<ScheduleIntent> _schedule = new(512);
    // DisconnectSystem
    private readonly StructBuffer<DisconnectIntent> _disconnect = new(16);

    public IntentBusContainer()
    {
        // ActionOrchestrator
        Action = new IntentWriter<ActionIntent>(_action);

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
        // AbilitySystem
        UseAbility = new IntentWriter<UseAbilityIntent>(_useAbility);
        ExecuteAbility = new IntentWriter<ExecuteAbilityIntent>(_executeAbility);
        // LootSystem
        CorpseLoot = new IntentWriter<CorpseLootIntent>(_corpseLoot);
        // LootSystem
        AutoSacrifice = new IntentWriter<AutoSacrificeIntent>(_autoSacrifice);
        // LookSystem
        Look = new IntentWriter<LookIntent>(_look);
        // ScheduleSystem
        Schedule = new IntentWriter<ScheduleIntent>(_schedule);
        // DisconnectSystem
        Disconnect = new IntentWriter<DisconnectIntent>(_disconnect);
    }

    // Action intents are a special case, we want to able to have direct access, because ActionOrchestrator add action intents while iterating them
    public ActionIntent ActionByIndex(int index) => _action[index];
    public int ActionCount => _action.Count;
    public IIntentWriter<ActionIntent> Action { get; }

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
    // AbilitySystem
    public IIntentWriter<UseAbilityIntent> UseAbility { get; }
    public Span<UseAbilityIntent> UseAbilitySpan => _useAbility.AsSpan();
    public IIntentWriter<ExecuteAbilityIntent> ExecuteAbility { get; }
    public Span<ExecuteAbilityIntent> ExecuteAbilitySpan => _executeAbility.AsSpan();
    // LootSystem
    public IIntentWriter<CorpseLootIntent> CorpseLoot { get; }
    public Span<CorpseLootIntent> CorpseLootSpan => _corpseLoot.AsSpan();
    // AutoSacrificeSystem
    public IIntentWriter<AutoSacrificeIntent> AutoSacrifice { get; }
    public Span<AutoSacrificeIntent> AutoSacrificeSpan => _autoSacrifice.AsSpan();
    // LookSystem
    public IIntentWriter<LookIntent> Look { get; }
    public Span<LookIntent> LookSpan => _look.AsSpan();
    // ScheduleSystem
    public IIntentWriter<ScheduleIntent> Schedule { get; }
    public Span<ScheduleIntent> ScheduleSpan => _schedule.AsSpan();
    // DisconnectSystem
    public IIntentWriter<DisconnectIntent> Disconnect { get; }
    public Span<DisconnectIntent> DisconnectSpan => _disconnect.AsSpan();

    public void ClearAll()
    {
        // ActionOrchestrator
        _action.Clear();

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
        // AbilitySystem
        _useAbility.Clear();
        _executeAbility.Clear();
        // LootSystem
        _corpseLoot.Clear();
        // LootSystem
        _autoSacrifice.Clear();
        // LookSystem
        _look.Clear();
        // ScheduleSystem
        _schedule.Clear();
        // DisconnectSystem
        _disconnect.Clear();
    }
}