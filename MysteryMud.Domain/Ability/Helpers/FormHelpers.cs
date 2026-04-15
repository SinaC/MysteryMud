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

        // Remove all usage tags
        e.Remove<UsesMana>();
        e.Remove<UsesRage>();
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
