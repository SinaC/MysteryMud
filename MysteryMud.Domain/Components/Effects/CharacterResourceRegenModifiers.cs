namespace MysteryMud.Domain.Components.Effects;

public struct CharacterResourceRegenModifiers<TResourceRegenModifier>
    where TResourceRegenModifier : struct
{
    public List<TResourceRegenModifier> Values;
}
