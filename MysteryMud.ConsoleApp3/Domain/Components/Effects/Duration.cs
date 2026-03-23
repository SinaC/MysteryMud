namespace MysteryMud.ConsoleApp3.Domain.Components.Effects;

struct Duration
{
    public long StartTick; // The tick at which the effect was applied
    public long ExpirationTick; // The tick at which the effect will expire
    public long? LastRefreshTick; // The last tick at which the duration was refreshed (for refreshing effects)
}
