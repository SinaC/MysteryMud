using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

static class HotSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
            .WithAll<EffectInstance, HealOverTime>();
        world.Query(in query, (Entity entity,
            ref EffectInstance effectInstance, ref HealOverTime hot) =>
        {
            //Console.WriteLine($"Processing HoT for Effect {entity.DisplayName} on Target {effectInstance.Target.DisplayName} with heal {hot.Heal} and tick rate {hot.TickRate} and stack count {effectInstance.StackCount}");

            if (effectInstance.Target.Has<DeadTag>())
            {
                Console.WriteLine($"Processing HoT for Effect {entity.DisplayName} on DEAD Target");
                return;
            }

            if (TimeSystem.CurrentTick < hot.NextTick)
                return;

            Console.WriteLine($"Applying HoT heal for Effect {entity.DisplayName} on Target {effectInstance.Target.DisplayName} with heal {hot.Heal} and tick rate {hot.TickRate} and stack count {effectInstance.StackCount}");

            hot.NextTick += hot.TickRate;

            var heal = hot.Heal * effectInstance.StackCount;
            HealSystem.ApplyHeal(world, effectInstance.Target, heal, effectInstance.Source);
        });
    }
}
