using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct EffectTagRef
{
    public enum TagKind : byte { Character, Item }

    public TagKind Kind { get; }
    public CharacterEffectTagId CharacterTag { get; }
    public ItemEffectTagId ItemTag { get; }

    public static EffectTagRef ForCharacter(CharacterEffectTagId tag) =>
        new(TagKind.Character, tag, ItemEffectTagId.None);
    public static EffectTagRef ForItem(ItemEffectTagId tag) =>
        new(TagKind.Item, CharacterEffectTagId.None, tag);

    private EffectTagRef(TagKind kind, CharacterEffectTagId c, ItemEffectTagId i)
    {
        Kind = kind;
        CharacterTag = c;
        ItemTag = i;
    }

    public override string ToString() => Kind switch
    {
        TagKind.Character => $"Character:{CharacterTag}",
        TagKind.Item => $"Item:{ItemTag}",
        _ => "Unknown"
    };
}
