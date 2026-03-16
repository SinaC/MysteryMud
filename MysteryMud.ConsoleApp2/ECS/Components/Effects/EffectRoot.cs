using Arch.Core;

namespace MysteryMud.ConsoleApp2.ECS.Components.Effects;

public struct EffectRoot
{
    public Entity Source;
    public Entity Target;
    public string SpellId;
}
