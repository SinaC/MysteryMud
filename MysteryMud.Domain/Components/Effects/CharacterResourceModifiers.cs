namespace MysteryMud.Domain.Components.Effects;

public struct CharacterResourceModifiers<TResourceModifier>
    where TResourceModifier : struct
{
    public List<TResourceModifier> Values;
}
