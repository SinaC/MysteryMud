namespace MysteryMud.Domain.Components.Effects;

public struct ResourceModifiers<TResourceModifier>
    where TResourceModifier : struct
{
    public List<TResourceModifier> Values;
}
