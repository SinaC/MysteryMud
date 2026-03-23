namespace MysteryMud.ConsoleApp3.Core;

public interface IGameLogger
{
    void Debug(string? message, params object?[] args);
    void Info(string? message, params object?[] args);
    void Warning(string? message, params object?[] args);
    void Error(string? message, params object?[] args);
    void Critical(string? message, params object?[] args);

    void Dot(string message, params object?[] args);

    void Hot(string message, params object?[] args);

    void Damage(string message, params object?[] args);

    void Heal(string message, params object?[] args);

    void Factory(string message, params object?[] args);

    void Cleanup(string message, params object?[] args);

    void Duration(string message, params object?[] args);
}
