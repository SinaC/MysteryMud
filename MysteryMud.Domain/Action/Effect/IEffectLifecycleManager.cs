using Arch.Core;
using MysteryMud.Core;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectLifecycleManager
{
    void RemoveEffect(GameState state, Entity effect);
}
