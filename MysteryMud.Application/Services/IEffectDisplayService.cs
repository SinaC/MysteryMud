using Arch.Core;
using MysteryMud.Core;

namespace MysteryMud.Application.Services;

public interface IEffectDisplayService
{
    void DisplayEffects(GameState state, Entity viewer, List<Entity> effects);
}
