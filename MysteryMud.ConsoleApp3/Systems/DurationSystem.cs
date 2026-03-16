using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DurationSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
                .WithAll<Effect, Duration>();
        world.Query(query, (Entity entity,
            ref Effect effect, ref Duration duration) =>
        {
            Console.WriteLine($"Processing Duration for Effect {entity.DisplayName} on Target {effect.Target.DisplayName} with remaining ticks {duration.RemainingTicks}");

            duration.RemainingTicks--;
            if (duration.RemainingTicks > 0)
                return;

            Console.WriteLine($"Wearing off Duration for Effect {entity.DisplayName} on Target {effect.Target.DisplayName}");

            ref var characterEffects = ref world.Get<CharacterEffects>(effect.Target);
            characterEffects.Effects.Remove(entity);

            if (!effect.Target.Has<DirtyStats>())
                effect.Target.Add<DirtyStats>();

            if (duration.WearOffMessage != null)
            {
                // TODO: in room ?
                MessageSystem.Send(effect.Target, duration.WearOffMessage);
            }

            world.Destroy(entity);
        });
    }
}
