using TinyECS;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct RespawnState
{
    public EntityId RespawnRoom;
    public EntityId Killer;
}
