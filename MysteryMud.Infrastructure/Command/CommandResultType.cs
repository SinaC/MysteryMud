namespace MysteryMud.Infrastructure.Command;

enum CommandResultType
{
    Success,
    NotFound,
    NoPermission,
    WrongPosition,
    Ambiguous
}
