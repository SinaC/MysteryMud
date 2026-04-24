using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class GendersExtensions
{
    extension(GenderKind gender)
    {
        public string Subject
            => gender switch
            {
                GenderKind.Male => "he",
                GenderKind.Female => "she",
                GenderKind.Neutral => "it",
                //GenderType.Neutral => "they",
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown value"),
            };

        public string Objective
            => gender switch
            {
                GenderKind.Male => "him",
                GenderKind.Female => "her",
                GenderKind.Neutral => "it",
                //GenderType.Neutral => "them",
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown value"),
            };

        public string Possessive
            => gender switch
            {
                GenderKind.Male => "its",
                GenderKind.Female => "hers",
                GenderKind.Neutral => "its",
                //GenderType.Neutral => "theirs",
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown value"),
            };
    }
}
