using MysteryMud.Domain.Action.Effect.Definitions;

namespace MysteryMud.Domain.Action.Effect.Factories;

public interface IEffectActionFactory
{
    public Action<EffectExecutionContext> Create(EffectActionDefinition actionDefinition);
}
