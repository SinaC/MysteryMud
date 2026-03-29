namespace MysteryMud.GameData.Enums;

public enum LookMode
{
    Snapshot, // only look at the current state of the target (used by the look command)
    PostUpdate, // look at the target after all updates have been processed, so you see the results of any changes that were made during the tick (used by autolook)
}
