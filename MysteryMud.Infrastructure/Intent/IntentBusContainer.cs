using MysteryMud.Core.Intent;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Infrastructure.Intent;

public sealed class IntentBusContainer : IIntentContainer
{
    // FleeSystem
    private readonly StructBuffer<FleeIntent> _flee = new(128);
    // MoveSystem
    private readonly StructBuffer<MoveIntent> _move = new(256);
    // InteractionSystem
    private readonly StructBuffer<GetItemIntent> _getItem = new(128);
    private readonly StructBuffer<DropItemIntent> _dropItem = new(128);
    private readonly StructBuffer<GiveItemIntent> _giveItem = new(128);
    private readonly StructBuffer<PutItemIntent> _putItem = new(128);
    // CombatSystem
    private readonly StructBuffer<AttackIntent> _attack1 = new(1024); // 2 buffers for double buffering
    private readonly StructBuffer<AttackIntent> _attack2 = new(1024);
    private StructBuffer<AttackIntent> _currentAttack; // points to the buffer for the current tick
    private StructBuffer<AttackIntent> _nextAttack; // points to the buffer for the next tick
    // LootSystem
    private readonly StructBuffer<LootIntent> _loot = new(128);

    // Writers for double buffering of AttackIntents
    private IIntentWriter<AttackIntent> _currentAttackWriter;
    private IIntentWriter<AttackIntent> _nextAttackWriter;

    public IntentBusContainer()
    {
        // FleeSystem
        Flee = new IntentWriter<FleeIntent>(_flee);
        // MoveSystem
        Move = new IntentWriter<MoveIntent>(_move);
        // InteractionSystem
        GetItem = new IntentWriter<GetItemIntent>(_getItem);
        DropItem = new IntentWriter<DropItemIntent>(_dropItem);
        GiveItem = new IntentWriter<GiveItemIntent>(_giveItem);
        PutItem = new IntentWriter<PutItemIntent>(_putItem);
        // CombatSystem
        _currentAttack = _attack1;
        _nextAttack = _attack2;
        _currentAttackWriter = new IntentWriter<AttackIntent>(_attack1);
        _nextAttackWriter = new IntentWriter<AttackIntent>(_attack2);
        // LootSystem
        Loot = new IntentWriter<LootIntent>(_loot);
    }

    // FleeSystem
    public IIntentWriter<FleeIntent> Flee { get; }
    public Span<FleeIntent> FleeSpan => _flee.AsSpan();
    // MoveSystem
    public IIntentWriter<MoveIntent> Move { get; }
    public Span<MoveIntent> MoveSpan => _move.AsSpan();
    // InteractionSystem
    public IIntentWriter<GetItemIntent> GetItem { get; }
    public Span<GetItemIntent> GetItemSpan => _getItem.AsSpan();
    public IIntentWriter<DropItemIntent> DropItem { get; }
    public Span<DropItemIntent> DropItemSpan => _dropItem.AsSpan();
    public IIntentWriter<GiveItemIntent> GiveItem { get; }
    public Span<GiveItemIntent> GiveItemSpan => _giveItem.AsSpan();
    public IIntentWriter<PutItemIntent> PutItem { get; }
    public Span<PutItemIntent> PutItemSpan => _putItem.AsSpan();
    // CombatSystem
    public IIntentWriter<AttackIntent> Attack => _currentAttackWriter; // can write to current attack buffer
    public Span<AttackIntent> AttackSpan => _currentAttack.AsSpan(); // can only iterate on the current attack buffer, not the next buffer
    public IIntentWriter<AttackIntent> AttackNext => _nextAttackWriter; // can write to next attack buffer, for counterattacks and multiattack combos
    // LootSystem
    public IIntentWriter<LootIntent> Loot { get; }
    public Span<LootIntent> LootSpan => _loot.AsSpan();

    public void SwapAttackBuffers()
    {
        (_nextAttack, _currentAttack) = (_currentAttack, _nextAttack);
        (_nextAttackWriter, _currentAttackWriter) = (_currentAttackWriter, _nextAttackWriter);
    }

    public void ClearAll()
    {
        // FleeSystem
        _flee.Clear();
        // MoveSystem
        _move.Clear();
        // InteractionSystem
        _getItem.Clear();
        _dropItem.Clear();
        _giveItem.Clear();
        _putItem.Clear();
        // CombatSystem
        _currentAttack.Clear();
        _nextAttack.Clear();
        // LootSystem
        _loot.Clear();
    }
}