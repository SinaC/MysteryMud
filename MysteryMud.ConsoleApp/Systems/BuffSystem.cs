using Arch.Core;
using MysteryMud.ConsoleApp.Components.Effects;

namespace MysteryMud.ConsoleApp.Systems
{
    static class BuffSystem
    {
        static QueryDescription hasteQuery = new QueryDescription().WithAll<Haste>();
        static QueryDescription gsQuery = new QueryDescription().WithAll<GiantStrength>();
        static QueryDescription sanctQuery = new QueryDescription().WithAll<Sanctuary>();

        public static void Run(World world, float dt, CommandBuffer cmd)
        {
            ApplyBuffDuration(world, hasteQuery, dt, cmd);
            ApplyBuffDuration(world, gsQuery, dt, cmd);
            ApplyBuffDuration(world, sanctQuery, dt, cmd);
        }

        static void ApplyBuffDuration(World world, QueryDescription q, float dt, CommandBuffer cmd)
        {
            //world.Query(q, (Entity e, dynamic buff) =>
            //{
            //    buff.Duration -= dt;
            //    if (buff.Duration <= 0) cmd.Add(w => w.Remove(buff.GetType(), e));
            //});
        }
    }
}
