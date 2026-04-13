using MysteryMud.Domain.Action.Effect.Definitions;

namespace MysteryMud.Domain.Action.Effect.Factories;

public interface IEffectRuntimeFactory
{
    public EffectRuntime Create(EffectDefinition def);
}
