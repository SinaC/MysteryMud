using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Domain.Action.Effect;

public static class EffectActionRegistry
{
    private static readonly Dictionary<Type, EffectTargetKind> _allowedTargets;

    static EffectActionRegistry()
    {
        _allowedTargets = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(EffectActionDefinition).IsAssignableFrom(t) && !t.IsAbstract)
            .ToDictionary(
                t => t,
                t => t.GetCustomAttribute<EffectActionTargetAttribute>()?.AllowedTargets
                     ?? EffectTargetKind.Character // safe default: character-only
            );
    }

    public static EffectTargetKind GetAllowedTargets(EffectActionDefinition action)
        => _allowedTargets.TryGetValue(action.GetType(), out var k) ? k : EffectTargetKind.Character;

    public static EffectTargetKind GetAllowedTargets<T>()
        where T : EffectActionDefinition
        => _allowedTargets.TryGetValue(typeof(T), out var k) ? k : EffectTargetKind.Character;
}
