namespace MysteryMud.Core.Command;

public enum CommandParseMode
{
    None, // inventory
    Target, // kill goblin
    TargetPair, // get sword from chest
    TargetTriple, // not currently used, but would be for commands like "give sword to guard"
    TargetAndText, // tell goblin hello
    TargetPairAndText, // not currently used, but would be for commands like "give sword to guard with note hello"
    FullText // say hello world
}
