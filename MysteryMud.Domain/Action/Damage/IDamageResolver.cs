using MysteryMud.Core;
using MysteryMud.Core.Effects;

namespace MysteryMud.Domain.Action.Damage;

public interface IDamageResolver
{
    DamageResult Resolve(GameState state, DamageAction action);
}