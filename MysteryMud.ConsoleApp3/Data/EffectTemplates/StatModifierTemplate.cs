using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Effects;

namespace MysteryMud.ConsoleApp3.Data.EffectTemplates;

public class StatModifierTemplate : IEffectTemplate
{
    public List<StatModifier> Modifiers = new();

    public void Apply(World world, Entity effectEntity)
    {
        effectEntity.Add(new StatModifiers
        {
            Values = [.. Modifiers] // copy modifiers to avoid reference issues
        });
    }
}
