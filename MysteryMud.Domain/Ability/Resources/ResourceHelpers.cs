using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Helpers;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Resources;

public static class ResourceHelpers
{
    public static bool CanPayCosts(Entity e, AbilityRuntime ability, out CannotPayCostsResult result)
    {
        foreach (var cost in ability.Costs)
        {
            // Handle form auto-switch (druid switches back to humanoid when casting a spell)
            if (cost.Kind == ResourceKind.Mana && !e.Has<UsesMana>())
            {
                FormHelpers.SwitchForm(e, FormType.Humanoid);
            }

            if (!CanUseResource(e, cost.Kind))
            {
                result = new CannotPayCostsResult
                {
                    Kind = cost.Kind,
                    Reason = CannotPayCostsReason.ResourceNotAvailable
                };
                return false;
            }

            if (!TryGetResource(e, cost.Kind, out var current, out _))
            {
                result = new CannotPayCostsResult
                {
                    Kind = cost.Kind,
                    Reason = CannotPayCostsReason.ResourceNotAvailable
                };
                return false;
            }

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

    public static void PayCosts(Entity e, AbilityRuntime skill)
    {
        foreach (var cost in skill.Costs)
        {
            var finalCost = ResolveCost(e, cost);
            ModifyResource(e, cost.Kind, -finalCost);
        }
    }

    public static int ResolveCost(Entity e, ResourceCost cost)
    {
        var ctx = new CostContext
        {
            Entity = e,
            Kind = cost.Kind,
            BaseAmount = cost.Amount,
            FinalAmount = cost.Amount
        };

        // TODO: RunCostModifiers(ref ctx);

        return ctx.FinalAmount;
    }

    public static bool TryGetResource(Entity e, ResourceKind type, out int current, out int max)
    {
        switch (type)
        {
            case ResourceKind.Mana:
                if (e.Has<Mana>())
                {
                    ref var m = ref e.Get<Mana>();
                    current = m.Current;
                    max = m.Max;
                    return true;
                }
                break;

            case ResourceKind.Rage:
                if (e.Has<Rage>())
                {
                    ref var r = ref e.Get<Rage>();
                    current = r.Current;
                    max = r.Max;
                    return true;
                }
                break;

            case ResourceKind.Energy:
                if (e.Has<Energy>())
                {
                    ref var en = ref e.Get<Energy>();
                    current = en.Current;
                    max = en.Max;
                    return true;
                }
                break;
        }

        current = max = 0;
        return false;
    }

    public static void ModifyResource(Entity e, ResourceKind type, int delta)
    {
        switch (type)
        {
            case ResourceKind.Mana:
                ref var m = ref e.Get<Mana>();
                m.Current += delta;
                break;

            case ResourceKind.Rage:
                ref var r = ref e.Get<Rage>();
                r.Current += delta;
                break;

            case ResourceKind.Energy:
                ref var en = ref e.Get<Energy>();
                en.Current += delta;
                break;
        }
    }

    public static bool CanUseResource(Entity e, ResourceKind type)
    {
        return type switch
        {
            ResourceKind.Mana => e.Has<UsesMana>(),
            ResourceKind.Rage => e.Has<UsesRage>(),
            ResourceKind.Energy => e.Has<UsesEnergy>(),
            _ => false
        };
    }
}
