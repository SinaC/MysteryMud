using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;

namespace MysteryMud.Domain.Action.Move;

public class MoveResolver : IMoveResolver
{
    private readonly IGameMessageService _msg;

    public MoveResolver(IGameMessageService msg)
    {
        _msg = msg;
    }

    public RestoreMoveResult Resolve(GameState state, RestoreMoveAction action) // to be used during combat process
    {
        var source = action.Source;
        var target = action.Target;
        var amount = action.Amount;
        if (!target.IsAlive() || target.Has<Dead>()) // already dead
            return new RestoreMoveResult { IsSuccess = false };

        ref var move = ref target.TryGetRef<Components.Characters.Move>(out var hasMove);
        if (!hasMove)
            return new RestoreMoveResult { IsSuccess = false };

        if (move.Current >= move.Max) // already at max hp
            return new RestoreMoveResult
            {
                IsSuccess = false,
                MaxMove = true
            };

        // apply move modifiers
        decimal modifiedMove = MoveCalculator.ModifyRestoreMove(target, amount, source);
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
