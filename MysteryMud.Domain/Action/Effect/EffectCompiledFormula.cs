namespace MysteryMud.Domain.Action.Effect;

public class EffectCompiledFormula
{
    //TODO: AST public required FormulaNode Root { get; init; }
    public required Func<EffectContext, decimal> Compiled { get; init; }
    public required string OriginalExpression { get; init; }
}
