using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class GendersExtensions
{
    extension(Genders gender)
    {
        public string Subject
               => gender switch
               {
                   Genders.Male => "he",
                   Genders.Female => "she",
                   Genders.Neutral => "it",
                   //GenderType.Neutral => "they",
                   _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown value"),
               };

        public string Objective
            => gender switch
            {
                Genders.Male => "him",
                Genders.Female => "her",
                Genders.Neutral => "it",
                //GenderType.Neutral => "them",
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown value"),
            };

        public string Possessive
            => gender switch
            {
                Genders.Male => "its",
                Genders.Female => "hers",
                Genders.Neutral => "its",
                //GenderType.Neutral => "theirs",
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown value"),
            };
    }
}
