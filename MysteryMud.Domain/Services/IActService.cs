using DefaultEcs;

namespace MysteryMud.Domain.Services;

public interface IActService
{
    string FormatFor(Entity viewer, string format, params object[] args);
}
