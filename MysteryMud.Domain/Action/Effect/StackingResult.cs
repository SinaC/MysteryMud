namespace MysteryMud.Domain.Action.Effect;

public enum StackingResult
{
    Nop, // don't do anything
    NoExisting, // create new effect
    DifferentSource, // create new effect
    Refreshed, // don't do anything
    Stacked, // flag as Dirty
    Replaced, // create new effect
}