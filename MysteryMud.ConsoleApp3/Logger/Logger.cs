using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MysteryMud.ConsoleApp3.Data.Definitions;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using Serilog;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;

namespace MysteryMud.ConsoleApp3.Logger;

public static class Logger
{
    // Static logger instance, initialized once at startup
    public static Microsoft.Extensions.Logging.ILogger Instance { get; private set; } = default!;

    // Predefined EventIds per category
    public static readonly EventId SystemEvent = new(1, "System");
    public static readonly EventId DotEvent = new(2, "Dot");
    public static readonly EventId HotEvent = new(3, "Hot");
    public static readonly EventId DamageEvent = new(4, "Damage");
    public static readonly EventId HealEvent = new(5, "Heal");
    public static readonly EventId FactoryEvent = new(6, "Factory");
    public static readonly EventId CleanupEvent = new(7, "Cleanup");
    public static readonly EventId DurationEvent = new(8, "Duration");

    // Initialize the logger once
    public static void Initialize(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // Set the minimum level
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(); // Use Serilog as provider
        });

        Instance = factory.CreateLogger("MysteryMud");

        Instance.LogInformation(SystemEvent, "Log initialized");
    }

    public static void System(LogLevel logLevel, string? message, params object?[] args)
    {
        if (Instance.IsEnabled(logLevel))
            Instance.LogInformation(SystemEvent, message, args);
    }

    // Zero-allocation structured log methods
    public static void Respawn(Entity player, Entity room)
    {
        if (Instance.IsEnabled(LogLevel.Information))
            Instance.LogInformation(SystemEvent, "Respawning character {playerName} to room {roomName}", player.DebugName, room.DebugName);
    }

    public static class Factory
    {
        public static void CreateEffect(Entity source, Entity target, EffectTemplate effectTemplate)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, "Creating Effect from Template {effectTemplateName} Source {sourceName} Target {targetName}", effectTemplate.Name, source.DebugName, target.DebugName);
        }

        public static void AddTagToEffect(EffectTagId tag)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, " - add tag {tag}", tag);
        }

        public static void AddDurationToEffect(long duration, long expirationTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, " - add duration {duration} ticks (expires at {expirationTick})", duration, expirationTick);
        }

        public static void AddStatModifiersToEffect(IEnumerable<StatModifier> modifiers)
        {
            if (Instance.IsEnabled(LogLevel.Information))
            {
                foreach (var mod in modifiers)
                    Instance.LogInformation(FactoryEvent, " - add stat modifier {stat} {value} ({type})", mod.Stat, mod.Value, mod.Type);
            }
        }

        public static void AddDotToEffect(int damage, DamageType damageType, long tickRate, long nextTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, " - add DoT {damage} damage of type {damageType} with tick rate {tickRate} and next tick {nextTick}", damage, damageType, tickRate, nextTick);
        }

        public static void AddHotToEffect(int heal, long tickRate, long nextTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, " - add HoT {heal} heal with tick rate {tickRate} and next tick {nextTick}", heal, tickRate, nextTick);
        }

        public static void RefreshEffect(Entity source, Entity target, EffectTemplate effectTemplate, long duration, long expirationTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, "Refreshing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration} Expiration {expirationTick}", effectTemplate.Name, source.DebugName, target.DebugName, duration, expirationTick);
        }

        public static void StackEffect(Entity source, Entity target, EffectTemplate effectTemplate, long duration, long expirationTick, int newStackCount)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(FactoryEvent, "Stacking/Refreshing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration} Expiration {expirationTick} New Stack Count {newStackCount}", effectTemplate.Name, source.DebugName, target.DebugName, duration, expirationTick, newStackCount);
        }
    }

    public static class Cleanup
    {
        public static void CleanupPlayer(Entity player)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(CleanupEvent, "Cleaning up disconnected player {characterName}", player.DebugName);
        }

        public static void CleanupCharacterFromRoom(Entity character, Entity room)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(CleanupEvent, "Cleaning up character {characterName} from room {roomName}", character.DebugName, room.DebugName);
        }

        public static void CleanupItemFromRoom(Entity item, Entity location)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(CleanupEvent, "Cleaning up item {itemName} from location {locationName}", item.DebugName, location.DebugName);
        }

        public static void CleanupItemFromInventory(Entity item, Entity inventoryOwner)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(CleanupEvent, "Cleaning up item {itemName} from inventory of {inventoryOwnerName}", item.DebugName, inventoryOwner.DebugName);
        }

        public static void CleanupItemFromContainer(Entity item, Entity container)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(CleanupEvent, "Cleaning up item {itemName} from container {containerName}", item.DebugName, container.DebugName);
        }

        public static void CleanupItemFromEquipment(Entity item, Entity wearer, EquipmentSlot slot)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(CleanupEvent, "Cleaning up item {itemName} from equipment of {wearerName} in slot {slot}", item.DebugName, wearer.DebugName, slot);
        }
    }

    public static class Duration
    {
        public static void Reschedule(Entity effect, Entity target, long expirationTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DurationEvent, "Rescheduled Duration for Effect {effectName} on Target {targetName} with Expiration Tick {expirationTick}", effect.DebugName, target.DebugName, expirationTick);
        }
        public static void Expire(Entity effect, Entity target)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DurationEvent, "Expiring Duration for Effect {effectName} on Target {targetName}", effect.DebugName, target.DebugName);
        }
    }

    public static class Heal
    {
        public static void Apply(Entity source, Entity target, int heal, ref Health health)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(HealEvent, "Applying heal from {sourceName} to {targetName} with amount {heal}. Current health: {health.Current}/{health.Max}", source.DebugName, target.DebugName, heal, health.Current, health.Max);
        }
    }

    public static class Damage
    {
        public static void Apply(Entity source, Entity target, int damage, ref Health health)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DamageEvent, "Applying damage from {sourceName} to {targetName} with amount {damage}. Current health: {health.Current}/{health.Max}", source.DebugName, target.DebugName, damage, health.Current, health.Max);
        }

        public static void TargetKilled(Entity source, Entity target)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DamageEvent, "Target {targetName} killed by {sourceName}", target.DebugName, source.DebugName);
        }
    }

    public static class Dot
    {
        public static void TickOnDeadTarget(Entity effect, Entity target)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DotEvent, "Ticking DoT for Effect {effectName} on DEAD Target {targetName}", effect.DebugName, target.DebugName);
        }

        public static void TickAfterExpirationTime(Entity effect, Entity target, long tickRate)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DotEvent, "Ticking DoT for Effect {effectName} on Target {targetName} and tick rate {tickRate} on EXPIRED effect", effect.DebugName, target.DebugName, tickRate);
        }

        public static void ApplyDamage(Entity effect, Entity target, int damage, DamageType damageType, long tickRate)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DotEvent, "Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageType} and tick rate {tickRate}", effect.DebugName, target.DebugName, damage, damageType, tickRate);
        }

        public static void TargetKilled(Entity effect, Entity target)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DotEvent, "Target {targetName} died from DoT damage of Effect {effectName}", target.DebugName, effect.DebugName);
        }

        public static void ScheduleNextTick(Entity effect, Entity target, long nextTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(DotEvent, "Scheduling next DoT tick for Effect {effectName} on Target {targetName} at tick {nextTick}", effect.DebugName, target.DebugName, nextTick);
        }
    }

    public static class Hot
    {
        public static void TickOnDeadTarget(Entity effect, Entity target)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(HotEvent, "Ticking HoT for Effect {effectName} on DEAD Target {targetName}", effect.DebugName, target.DebugName);
        }

        public static void TickAfterExpirationTime(Entity effect, Entity target, long tickRate)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(HotEvent, "Ticking HoT for Effect {effectName} on Target {targetName} and tick rate {tickRate} on EXPIRED effect", effect.DebugName, target.DebugName, tickRate);
        }

        public static void ApplyHeal(Entity effect, Entity target, int heal, long tickRate)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(HotEvent, "Applying HoT heal for Effect {effectName} on Target {targetName} with heal {heal} and tick rate {tickRate}", effect.DebugName, target.DebugName, heal, tickRate);
        }

        public static void ScheduleNextTick(Entity effect, Entity target, long nextTick)
        {
            if (Instance.IsEnabled(LogLevel.Information))
                Instance.LogInformation(HotEvent, "Scheduling next HoT tick for Effect {effectName} on Target {targetName} at tick {nextTick}", effect.DebugName, target.DebugName, nextTick);
        }
    }
}
