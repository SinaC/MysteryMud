namespace MysteryMud.ConsoleApp3.Data.Enums;

enum EventType // order is important, when multiple events are scheduled for the same tick, they will be executed in the order of their type value
{
    DotTick = 10, // This event type is used to indicate that a damage over time (DoT) effect should apply its damage to the target.
    HotTick = 20, // This event type is used to indicate that a heal over time (HoT) effect should apply its healing to the target.

    EffectExpired = 100, // This event type is used to indicate that an effect has expired and should be removed from the entity.
}
