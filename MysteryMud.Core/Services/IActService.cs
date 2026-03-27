using Arch.Core;

namespace MysteryMud.Core.Services;

public interface IActService
{
    string FormatFor(Entity viewer, string format, params object[] args);
}
