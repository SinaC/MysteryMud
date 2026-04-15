using Arch.Core;
using MysteryMud.Domain.Formatters;

namespace MysteryMud.Domain.Services;

public class ActService : IActService
{
    public string FormatFor(Entity viewer, string format, params object[] args)
        => ActFormatter.FormatActOneLine(viewer, format, args);
}
