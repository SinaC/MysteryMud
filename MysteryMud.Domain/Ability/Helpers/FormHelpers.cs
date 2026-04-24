using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Helpers;

public static class FormHelpers
{
    public static void SwitchForm(World world, EntityId entity, FormType newForm)
    {
        ref var form = ref world.TryGetRef<Form>(entity, out var hasForm);
        if (!hasForm)
            return;

        if (form.Value == newForm)
            return;

        form.Value = newForm;

        // TODO: optimize to avoid removing and readding the same
        // Remove all usage tags
        if (world.Has<UsesMana>(entity))
            world.Remove<UsesMana>(entity);
        if (world.Has<UsesRage>(entity))
            world.Remove<UsesRage>(entity);
        if (world.Has<UsesEnergy>(entity))
            world.Remove<UsesEnergy>(entity);

        // Add what the form allows
        switch (newForm)
        {
            case FormType.Humanoid:
                world.Add<UsesMana>(entity);
                break;

            case FormType.Bear:
                world.Add<UsesRage>(entity);
                break;

            case FormType.Cat:
                world.Add<UsesEnergy>(entity);
                break;
        }
    }
}
