using Arch.Core;

namespace MysteryMud.Domain.Ability.Services;

public readonly struct TargetResolutionResult
{
    public readonly TargetResolutionStatus Status;
    public readonly List<Entity> Targets;

    // Human-readable reason sent to the caster on failure.
    public readonly string? FailureMessage;

    public static TargetResolutionResult Success(List<Entity> targets)
        => new(TargetResolutionStatus.Ok, targets, null);

    public static TargetResolutionResult Failure(TargetResolutionStatus status, string? message = null)
        => new(status, [], message);

    private TargetResolutionResult(TargetResolutionStatus status, List<Entity> targets, string? failureMessage)
    {
        Status = status;
        Targets = targets;
        FailureMessage = failureMessage;
    }
}
