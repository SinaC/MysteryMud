using MysteryMud.Core.Effects;
using MysteryMud.Domain.Services;

namespace MysteryMud.Domain.Action.Effect;

public readonly ref struct EffectExecutionContext
{
    public EffectContext Context { get; init; }
    public IEffectExecutor Executor { get; init; }
    public IGameMessageService Msg { get; init; }
}
