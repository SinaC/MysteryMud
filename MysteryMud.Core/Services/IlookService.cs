using Arch.Core;

namespace MysteryMud.Core.Services;

public interface ILookService
{
    void DescribeRoom(Entity viewer, Entity room);
    void DescribeCharacter(Entity viewer, Entity target);
    void DescribeItem(Entity viewer, Entity item);
}
