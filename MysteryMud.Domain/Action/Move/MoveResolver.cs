using MysteryMud.Core;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Domain.Action.Move;

public class MoveResolver : IMoveResolver
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public MoveResolver(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public RestoreMoveResult Resolve(GameState state, RestoreMoveAction action) // to be used during combat process
    {
        var source = action.Source;
        var target = action.Target;
        var amount = action.Amount;
        if (!CharacterHelpers.IsAlive(_world, target)) // already dead
            return new RestoreMoveResult { IsSuccess = false };

        ref var move = ref _world.TryGetRef<Components.Characters.Move>(target, out var hasMove);
        if (!hasMove)
            return new RestoreMoveResult { IsSuccess = false };

        if (move.Current >= move.Max) // already at max hp
            return new RestoreMoveResult
            {
                IsSuccess = false,
                MaxMove = true
            };

        // apply move modifiers
        decimal modifiedMove = MoveCalculator.ModifyRestoreMove(target, source, amount);
        // cap to max move-current
        decimal maxPossibleMove = move.Max - move.Current;
        decimal finalMove = Math.Min(modifiedMove, maxPossibleMove);
        // move to apply, apply rounding
        int moveToApply = (int)Math.Round(finalMove, MidpointRounding.AwayFromZero);

        // we have to split sending to source and sending to room because source may not be in the same room
        if (source == target)
            _msg.To(action.Source).Act("%gYou move yourself for {0} move.%x").With(moveToApply);
        else
        {
            _msg.To(source).Act("%gYou move {0:n} for {1} move.%x").With(target, moveToApply);
            _msg.To(target).Act("%g{0} move{0:v} you for {1} move.%x").With(source, moveToApply);
            _msg.ToRoomExcept(target, source).Act("%y{0} move{0:v} {1} for {2} move.%x").With(source, target, moveToApply);
        }

        // apply move
        move.Current += moveToApply;

        return new RestoreMoveResult
        {
            IsSuccess = true,
            Amount = amount,
            MaxMove = move.Current == move.Max,
        };
    }
}
