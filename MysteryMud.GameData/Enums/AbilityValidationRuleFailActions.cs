namespace MysteryMud.GameData.Enums;

[Flags]
public enum AbilityValidationRuleFailActions
{
    None = 0,
    SkipTarget = 1 << 0,
    DisplayMessage = 1 << 1
}
