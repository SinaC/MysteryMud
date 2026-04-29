using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Services;

public interface IResistanceService
{
    ResistanceLevels CheckResistance(Entity victim, DamageKind damageKind);
}