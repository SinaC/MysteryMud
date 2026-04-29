using DefaultEcs;

namespace MysteryMud.GameData.Events;

public struct ExperienceGrantedEvent
{
    public Entity Target;
    public long Gain; // can be negative
}
