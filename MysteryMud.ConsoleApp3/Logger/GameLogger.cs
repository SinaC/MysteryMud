using Microsoft.Extensions.Logging;
using MysteryMud.ConsoleApp3.Core;
using Serilog.Core;

namespace MysteryMud.ConsoleApp3.Logger;

public class GameLogger : IGameLogger
{
    private readonly ILogger _logger;

    public GameLogger(ILogger logger)
    {
        _logger = logger;
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Debug(string? message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(LogEvents.System, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Info(string? message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.System, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Warning(string? message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
            _logger.LogWarning(LogEvents.System, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Error(string? message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Error))
            _logger.LogError(LogEvents.System, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Critical(string? message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Critical))
            _logger.LogCritical(LogEvents.System, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Dot(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Dot, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Hot(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Hot, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Damage(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Damage, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Heal(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Heal, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Factory(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Factory, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Cleanup(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Cleanup, message, args);
    }

    [MessageTemplateFormatMethod(nameof(message))]
    public void Duration(string message, params object?[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(LogEvents.Duration, message, args);
    }

    // optimized log example with precompiled message template, but less flexible and more verbose to use
    //private static readonly Action<ILogger, int, int, Exception?> _damageLog =
    //LoggerMessage.Define<int, int>(
    //    LogLevel.Information,
    //    LogEvents.Damage,
    //    "Entity {EntityId} takes {Damage} damage");

    //public void Damage(int entityId, int damage)
    //{
    //    _damageLog(_logger, entityId, damage, null);
    //}
}

public static class LogEvents
{
    public static readonly EventId System = new(1, "System");
    public static readonly EventId Dot = new(2, "Dot");
    public static readonly EventId Hot = new(3, "Hot");
    public static readonly EventId Damage = new(4, "Damage");
    public static readonly EventId Heal = new(5, "Heal");
    public static readonly EventId Factory = new(6, "Factory");
    public static readonly EventId Cleanup = new(7, "Cleanup");
    public static readonly EventId Duration = new(8, "Duration");
}