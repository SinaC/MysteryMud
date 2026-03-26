using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Services;

public interface IActService
{
    void Send(Entity actor, ActTargetOptions option, string format, params object[]? args);
    void Send(Entity actor, ActTargetOptions option, string format, Positions minPosition = Positions.Resting, params object[]? args);
    void Send(IEnumerable<Entity> targets, string format, params object[]? args);
}
