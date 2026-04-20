using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Helpers;

public static class FormHelpers
{
    public static void SwitchForm(Entity e, FormType newForm)
    {
        ref var form = ref e.TryGetRef<Form>(out var hasForm);
        if (!hasForm)
            return;

        if (form.Value == newForm)
            return;

        form.Value = newForm;

        // TODO: optimize to avoid removing and readding the same
        // Remove all usage tags
        if (e.Has<UsesMana>())
            e.Remove<UsesMana>();
        if (e.Has<UsesRage>())
            e.Remove<UsesRage>();
        if (e.Has<UsesEnergy>())
            e.Remove<UsesEnergy>();

        // Add what the form allows
        switch (newForm)
        {
            case FormType.Humanoid:
                e.Add<UsesMana>();
                break;

            case FormType.Bear:
                e.Add<UsesRage>();
                break;

            case FormType.Cat:
                e.Add<UsesEnergy>();
                break;
        }
    }
}
