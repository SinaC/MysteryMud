using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Helpers;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Resources;

public static class ResourceHelpers
{
    public static bool CanPayCosts(World world, EntityId entity, AbilityRuntime ability, out CannotPayCostsResult result)
    {
        foreach (var cost in ability.Costs)
        {
            // Handle form auto-switch (druid switches back to humanoid when casting a spell)
            if (cost.Kind == ResourceKind.Mana && !world.Has<UsesMana>(entity))
            {
                FormHelpers.SwitchForm(world, entity, FormType.Humanoid);
            }

            if (!CanUseResource(world, entity, cost.Kind))
            {
                result = new CannotPayCostsResult
                {
                    Kind = cost.Kind,
                    Reason = CannotPayCostsReason.ResourceNotAvailable
                };
                return false;
            }

            if (!TryGetResource(world, entity, cost.Kind, out var current, out _))
            {
                result = new CannotPayCostsResult
                {
                    Kind = cost.Kind,
                    Reason = CannotPayCostsReason.ResourceNotAvailable
                };
                return false;
            }

            // TODO: use ResolveCost ?

            if (current < cost.Amount)
            {
                result = new CannotPayCostsResult
                {
                    Kind = cost.Kind,
                    Reason = CannotPayCostsReason.NotEnoughResource
                };
                return false;
            }
        }

        result = default;
        return true;
    }

    public static void PayCosts(World world, EntityId entity, AbilityRuntime ability)
    {
        foreach (var cost in ability.Costs)
        {
            var finalCost = ResolveCost(entity, cost);
            ModifyResource(world, entity, cost.Kind, -finalCost);
        }
    }

    public static int ResolveCost(EntityId entity, ResourceCost cost)
    {
        var ctx = new CostContext
        {
            Entity = entity,
            Kind = cost.Kind,
            BaseAmount = cost.Amount,
            FinalAmount = cost.Amount
        };

        // TODO: RunCostModifiers(ref ctx);

        return ctx.FinalAmount;
    }

    public static bool TryGetResource(World world, EntityId entity, ResourceKind type, out int current, out int max)
    {
        switch (type)
        {
            case ResourceKind.Mana:
                if (world.Has<Mana>(entity))
                {
                    ref var m = ref world.Get<Mana>(entity);
                    current = m.Current;
                    max = m.Max;
                    return true;
                }
                break;

            case ResourceKind.Rage:
                if (world.Has<Rage>(entity))
                {
                    ref var r = ref world.Get<Rage>(entity);
                    current = r.Current;
                    max = r.Max;
                    return true;
                }
                break;

            case ResourceKind.Energy:
                if (world.Has<Energy>(entity))
                {
                    ref var en = ref world.Get<Energy>(entity);
                    current = en.Current;
                    max = en.Max;
                    return true;
                }
                break;
        }

        current = max = 0;
        return false;
    }

    public static void ModifyResource(World world, EntityId entity, ResourceKind type, decimal delta)
    {
        switch (type)
        {
            case ResourceKind.Mana:
                ref var m = ref world.Get<Mana>(entity);
                m.Current = (int)Math.Round(Math.Clamp(m.Current + delta, 0, m.Max), MidpointRounding.AwayFromZero);
                break;

            case ResourceKind.Rage:
                ref var r = ref world.Get<Rage>(entity);
                r.Current = (int)Math.Round(Math.Clamp(r.Current + delta, 0, r.Max), MidpointRounding.AwayFromZero);
                break;

            case ResourceKind.Energy:
                ref var en = ref world.Get<Energy>(entity);
                en.Current = (int)Math.Round(Math.Clamp(en.Current + delta, 0, en.Max), MidpointRounding.AwayFromZero);
                break;
        }
    }

    public static bool CanUseResource(World world, EntityId entity, ResourceKind type)
    {
        return type switch
        {
            ResourceKind.Mana => world.Has<UsesMana>(entity),
            ResourceKind.Rage => world.Has<UsesRage>(entity),
            ResourceKind.Energy => world.Has<UsesEnergy>(entity),
            _ => false
        };
    }
}
