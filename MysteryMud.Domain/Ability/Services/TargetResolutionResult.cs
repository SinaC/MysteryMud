using TinyECS;

namespace MysteryMud.Domain.Ability.Services;

public readonly struct TargetResolutionResult
{
    public readonly TargetResolutionStatus Status;
    public readonly List<EntityId> Targets;

    // Human-readable reason sent to the caster on failure.
    public readonly string? FailureMessage;

    public static TargetResolutionResult Success(List<EntityId> targets)
        => new(TargetResolutionStatus.Ok, targets, null);

    public static TargetResolutionResult Failure(TargetResolutionStatus status, string? message = null)
        => new(status, [], message);

    private TargetResolutionResult(TargetResolutionStatus status, List<EntityId> targets, string? failureMessage)
    {
        Status = status;
        Targets = targets;
        FailureMessage = failureMessage;
    }
}
