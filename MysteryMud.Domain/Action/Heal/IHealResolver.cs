using MysteryMud.Core;
using MysteryMud.Core.Effects;

namespace MysteryMud.Domain.Action.Heal;

public interface IHealResolver
{
    HealResult Resolve(GameState state, HealAction action);
}