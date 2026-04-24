using TinyECS;

namespace MysteryMud.Domain.Services;

public interface IActService
{
    string FormatFor(EntityId viewer, string format, params object[] args);
}
