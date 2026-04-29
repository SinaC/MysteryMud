using DefaultEcs;

namespace MysteryMud.Domain.Services;

public interface ISacrificeService
{
    void Sacrifice(Entity actor, Entity item);
}
