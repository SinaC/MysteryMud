using TinyECS;

namespace MysteryMud.Domain.Services;

public interface ISacrificeService
{
    void Sacrifice(EntityId actor, EntityId item);
}
