using Arch.Core;

namespace MysteryMud.ConsoleApp3.Data.EffectTemplates;

public interface IEffectTemplate
{
    void Apply(World world, Entity effectEntity);
}
