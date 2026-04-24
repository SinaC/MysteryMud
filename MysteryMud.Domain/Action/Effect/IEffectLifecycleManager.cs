using TinyECS;
using MysteryMud.Core;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectLifecycleManager
{
    void RemoveEffect(EntityId effect);
}
