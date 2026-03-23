using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Domain.Components.Items;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DestroySystem
{
    // destroy item and all contained items, if any
    public static void DestroyItem(Entity item)
    {
        item.Add<DestroyedTag>();

        if (item.Has<Container>())
        {
            var container = item.Get<ContainerContents>();
            foreach (var containedItem in container.Items)
            {
                DestroyItem(containedItem); // recursive call to destroy contained items
            }
        }

    }
}
