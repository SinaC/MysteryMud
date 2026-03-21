using Arch.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.ConsoleApp3.Commands.v2;

public static class CommandResolver
{
    public static void ResolveArguments(CommandContext ctx, World world)
    {
        if (ctx.Arguments.TryGetValue("item", out var itemObj) && itemObj is ItemArg item)
        {
            if (item.All)
            {
                // TODO
                //item.Entities = world.FindEntitiesByName(item.Name).ToList();
            }
            else if (item.Index.HasValue)
            {
                // TODO
                //var entities = world.FindEntitiesByName(item.Name).ToList();
                //if (item.Index.Value - 1 < entities.Count)
                //    item.Entities = new List<int> { entities[item.Index.Value - 1] };
            }
            else
            {
                // TODO
                //var entities = world.FindEntitiesByName(item.Name).ToList();
                //if (entities.Any())
                //    item.Entities = new List<int> { entities.First() };
            }
        }

        if (ctx.Arguments.TryGetValue("container", out var contObj) && contObj is ContainerArg container)
        {
            // TODO
            //var entities = world.FindEntitiesByName(container.Name).ToList();
            //if (entities.Any())
            //    container.EntityId = entities.First();
        }

        // Handle "from [container]" case
        if (ctx.Arguments.TryGetValue("item", out var itemArgObj) && itemArgObj is ItemArg itm
            && ctx.Arguments.TryGetValue("container", out var contArgObj) && contArgObj is ContainerArg ctr)
        {
            if (ctr.EntityId.HasValue)
            {
                // TODO
                //var itemsInContainer = world.FindEntitiesInContainer(ctr.EntityId.Value, itm.Name).ToList();
                //if (itm.All)
                //{
                //    itm.Entities = itemsInContainer;
                //}
                //else if (itm.Index.HasValue)
                //{
                //    if (itm.Index.Value - 1 < itemsInContainer.Count)
                //        itm.Entities = new List<int> { itemsInContainer[itm.Index.Value - 1] };
                //}
                //else
                //{
                //    if (itemsInContainer.Any())
                //        itm.Entities = new List<int> { itemsInContainer.First() };
                //}
            }
        }
    }
}
