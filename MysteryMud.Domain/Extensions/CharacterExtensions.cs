using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class CharacterExtensions
{
    extension(Entity entity)
    {
        public Positions Position => entity.TryGet<Position>(out var position) ? position.Value : Positions.Dead;
    }
}
