using TinyECS;

namespace MysteryMud.GameData.Events;

public struct ExperienceGrantedEvent
{
    public EntityId Target;
    public long Gain; // can be negative
}
