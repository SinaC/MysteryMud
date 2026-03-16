using Arch.Core;
using MysteryMud.ConsoleApp2.ECS.Components.Effects;

namespace MysteryMud.ConsoleApp2.ECS.Systems;

public static class DurationSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
            .WithAll<EffectRoot, EffectDuration>();

        world.Query(in query, (Entity e, ref EffectRoot root, ref EffectDuration duration) =>
        {
            if (GameTime.Tick >= duration.ExpireTick)
                ExpireEffect(world, e, root);
        });
    }

    private static void ExpireEffect(World world, Entity root, EffectRoot effect)
    {
        SendWearOff(effect);

        var q = new QueryDescription()
            .WithAll<EffectParent>();

        world.Query(in q, (Entity e, ref EffectParent parent) =>
        {
            if (parent.Root == root)
                world.Destroy(e);
        });

        world.Destroy(root);
    }

    private static void SendWearOff(EffectRoot effectRoot)
    {
        // TODO
    }
}
