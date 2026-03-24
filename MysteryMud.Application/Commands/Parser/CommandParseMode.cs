namespace MysteryMud.Application.Commands.Parser;

public enum CommandParseMode
{
    None, // inventory
    Target, // kill goblin
    TargetPair, // get sword from chest
    TargetAndText, // tell goblin hello
    FullText // say hello world
}
