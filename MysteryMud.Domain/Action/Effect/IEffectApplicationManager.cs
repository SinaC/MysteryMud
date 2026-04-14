using MysteryMud.Core;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectApplicationManager
{
    public void CreateEffect(GameState state, EffectRuntime effectRuntime, ref EffectData effectData);
}
