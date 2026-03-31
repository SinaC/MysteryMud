namespace MysteryMud.Domain.Components.Effects;

public struct TimedEffect
{
    public long TickRate; // How many ticks between each damage application
    public long NextTick; // The tick at which the next damage application will occur

    public long StartTick; // The tick at which the effect was applied
    public long ExpirationTick; // The tick at which the effect will expire
    public long? LastRefreshTick; // The last tick at which the duration was refreshed (for refreshing effects)
}
