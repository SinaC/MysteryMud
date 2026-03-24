namespace MysteryMud.Application.Commands.Registry;

enum CommandResultType
{
    Success,
    NotFound,
    NoPermission,
    WrongPosition,
    Ambiguous
}
