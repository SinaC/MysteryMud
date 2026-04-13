namespace MysteryMud.GameData.Enums;

public enum EffectFormulaEvaluationMode
{
    Dynamic,     // re-evaluate every tick (current behavior, live stats)
    Snapshotted, // snapshot inputs on apply, evaluate formula against snapshot each tick
}
