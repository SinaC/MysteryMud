using TinyECS;

namespace MysteryMud.Core.Services;

public interface ILookService
{
    void DescribeRoom(EntityId viewer, EntityId room);
    void DescribeCharacter(EntityId viewer, EntityId target);
    void DescribeItem(EntityId viewer, EntityId item);
}
