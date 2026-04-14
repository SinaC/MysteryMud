namespace MysteryMud.Domain.Components.Effects;

public struct ResourceRegenModifiers<TResourceRegenModifier>
    where TResourceRegenModifier : struct
{
    public List<TResourceRegenModifier> Values;
}
