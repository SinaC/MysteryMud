using TinyECS;
using MysteryMud.Core;

namespace MysteryMud.Application.Services;

public interface IEffectDisplayService
{
    void DisplayEffects(GameState state, EntityId viewer, List<EntityId> effects);
}
