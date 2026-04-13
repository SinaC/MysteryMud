using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Services;

public class CompiledFormula
{
    //TODO: AST public required FormulaNode Root { get; init; }
    public required Func<EffectContext, decimal> Compiled { get; init; }
    public required string OriginalExpression { get; init; }
}
