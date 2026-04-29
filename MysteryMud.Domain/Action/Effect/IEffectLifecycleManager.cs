using DefaultEcs;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectLifecycleManager
{
    void RemoveEffect(Entity effect);
}
