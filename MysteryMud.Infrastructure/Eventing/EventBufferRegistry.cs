using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Buffers;

namespace MysteryMud.Infrastructure.Eventing;

public class EventBufferRegistry
{
    public EventBuffer<FleeBlockedEvent> FleeBlocked { get; } = new();
    public EventBuffer<RoomEnteredEvent> RoomEntered { get; } = new();
    public EventBuffer<ItemGotEvent> ItemGot { get; } = new();
    public EventBuffer<ItemDroppedEvent> ItemDropped { get; } = new();
    public EventBuffer<ItemGivenEvent> ItemGiven { get; } = new();
    public EventBuffer<ItemPutEvent> ItemPut { get; } = new();
    public EventBuffer<ItemWornEvent> ItemWorn { get; } = new();
    public EventBuffer<ItemRemovedEvent> ItemRemoved { get; } = new();
    public EventBuffer<ItemDestroyedEvent> ItemDestroyed { get; } = new();
    public EventBuffer<ItemSacrificiedEvent> ItemSacrificed { get; } = new();
    public EventBuffer<DamagedEvent> Damaged { get; } = new();
    public EventBuffer<HealedEvent> Healed { get; } = new();
    public EventBuffer<DeathEvent> Death { get; } = new();
    public EventBuffer<ItemLootedEvent> ItemLooted { get; } = new();
    public EventBuffer<LookedEvent> Looked { get; } = new();
    public EventBuffer<TriggeredScheduledEvent> TriggeredScheduled { get; } = new();
    public EventBuffer<EffectExpiredEvent> EffectExpired { get; } = new();
    public EventBuffer<EffectTickedEvent> EffectTicked { get; } = new();
    public EventBuffer<AttackResolvedEvent> AttackResolved { get; } = new();
    public EventBuffer<EffectResolvedEvent> EffectResolved { get; } = new();
    public EventBuffer<AbilityUsedEvent> AbilityUsed { get; } = new();
    public EventBuffer<AbilityExecutedEvent> AbilityExecuted { get; } = new();
    public EventBuffer<ExperienceGrantedEvent> ExperienceGranted { get; } = new();
    public EventBuffer<LevelIncreasedEvent> LevelIncreased { get; } = new();
    public EventBuffer<KillRewardEvent> KillReward { get; } = new();
    public EventBuffer<AggressedEvent> Aggressed { get; } = new();

    public void ClearAll()
    {
        FleeBlocked.Clear();
        RoomEntered.Clear();
        ItemGot.Clear();
        ItemDropped.Clear();
        ItemGiven.Clear();
        ItemPut.Clear();
        ItemWorn.Clear();
        ItemRemoved.Clear();
        ItemDestroyed.Clear();
        ItemSacrificed.Clear();
        Damaged.Clear();
        Healed.Clear();
        Death.Clear();
        ItemLooted.Clear();
        Looked.Clear();
        TriggeredScheduled.Clear();
        EffectExpired.Clear();
        EffectTicked.Clear();
        AttackResolved.Clear();
        EffectResolved.Clear();
        AbilityUsed.Clear();
        AbilityExecuted.Clear();
        ExperienceGranted.Clear();
        LevelIncreased.Clear();
        KillReward.Clear();
        Aggressed.Clear();
    }
}
