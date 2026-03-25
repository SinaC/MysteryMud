namespace MysteryMud.Infrastructure.Command;

public enum CommandFindResultType
{
    Success,
    NotFound,
    NoPermission,
    WrongPosition,
    Ambiguous
}
