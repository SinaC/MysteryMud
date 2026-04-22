using MysteryMud.Core;
using MysteryMud.Core.Effects;

namespace MysteryMud.Domain.Action.Move;

public interface IMoveResolver
{
    RestoreMoveResult Resolve(GameState state, RestoreMoveAction action);
}