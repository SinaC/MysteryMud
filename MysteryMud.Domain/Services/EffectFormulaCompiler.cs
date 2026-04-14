using Arch.Core.Extensions;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MysteryMud.Domain.Services;

// can evaluate formulas like:
// 3 + caster.Level / 2 - range(1, 4)
// max(1, 2, range(1,5), caster.level, if(caster.level >= target.level && sum(1,2,3) < 10 || dice(2,6) == 12, -caster.strength, caster.level))
public class EffectFormulaCompiler
{
    // User-defined functions dictionary
    private readonly Dictionary<string, Func<decimal[], decimal>> _functions;

    public EffectFormulaCompiler()
    {
        _functions = new Dictionary<string, Func<decimal[], decimal>>(StringComparer.OrdinalIgnoreCase)
        {
            { "range", args =>
                {
                    if (args.Length != 2) throw new Exception("range() expects 2 arguments");
                    int min = (int)args[0], max = (int)args[1];
                    return Random.Shared.Next(min, max + 1);
                }
            },
            { "floor", args => Math.Floor(args[0]) },
            { "ceil", args => Math.Ceiling(args[0]) },
            { "round", args => Math.Round(args[0]) },
            { "sum", args => args.Sum() },
            { "max", args => args.Max() },
            { "min", args => args.Min() },
            // Example: dice(n,sides) rolls `n` dice with `sides` sides each
            { "dice", args =>
                {
                    if (args.Length != 2) throw new Exception("dice() expects 2 arguments");
                    int n = (int)args[0], sides = (int)args[1];
                    int total = 0;
                    for (int i = 0; i < n; i++) total += Random.Shared.Next(1, sides + 1);
                    return total;
                }
            },
            { "clamp", args =>
                {
                    if (args.Length != 3) throw new Exception("clamp() expects 3 arguments");
                    return Math.Clamp(args[0], args[1], args[2]);
                }
            },
            { "if", args =>
                {
                    if(args.Length != 3) throw new Exception("if() expects 3 arguments");
                    return args[0] != 0 ? args[1] : args[2];
                }
            },
            { "neg", args => -args[0]},
        };
    }

    public void AddFunction(string name, Func<decimal[], decimal> implementation)
    {
        _functions[name] = implementation;
    }

    public CompiledFormula Compile(string formula)
        => Compile(formula, EffectFormulaEvaluationMode.Snapshotted);

    public CompiledFormula Compile(
        string formula,
        EffectFormulaEvaluationMode mode)
    {
        var compiled = CompileInternal(formula);

        return new CompiledFormula
        {
            Compiled = ctx => compiled(ctx, mode),
            OriginalExpression = formula
        };
    }

    private Func<EffectContext, EffectFormulaEvaluationMode, decimal> CompileInternal(string formula)
    {
        var rpn = ToRpn(Tokenize(formula));

        return (ctx, mode) =>
        {
            var stack = new Stack<decimal>();

            ref var source = ref ctx.Source;
            ref var target = ref ctx.Target;

            ref var effectiveDamageAmount = ref ctx.EffectiveDamageAmount;

            var useSnapshot = mode == EffectFormulaEvaluationMode.Snapshotted && ctx.Effect.HasValue && ctx.Effect.Value.Has<EffectValuesSnapshot>();
            ref var snapshot = ref (useSnapshot
                ? ref ctx.Effect!.Value.Get<EffectValuesSnapshot>()
                : ref Unsafe.NullRef<EffectValuesSnapshot>());
            ref var casterLevel = ref(useSnapshot
                ? ref snapshot.SourceLevel
                : ref source.Get<Level>().Value);
            ref var targetLevel = ref (useSnapshot
                ? ref snapshot.TargetLevel
                : ref target.Get<Level>().Value);
            ref var casterStats = ref (useSnapshot
                ? ref snapshot.SourceStats
                : ref source.Get<EffectiveStats>().Values);
            ref var targetStats = ref (useSnapshot
                ? ref snapshot.TargetStats
                : ref target.Get<EffectiveStats>().Values);

            foreach (var token in rpn)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        stack.Push(token.Number);
                        break;

                    case TokenType.CasterLevel:
                        stack.Push(casterLevel);
                        break;

                    case TokenType.CasterStrength:
                        stack.Push(casterStats[StatKind.Strength]);
                        break;

                    case TokenType.CasterIntelligence:
                        stack.Push(casterStats[StatKind.Intelligence]);
                        break;

                    case TokenType.CasterWisdom:
                        stack.Push(casterStats[StatKind.Wisdom]);
                        break;

                    case TokenType.CasterDexterity:
                        stack.Push(casterStats[StatKind.Dexterity]);
                        break;

                    case TokenType.CasterConstitution:
                        stack.Push(casterStats[StatKind.Constitution]);
                        break;

                    case TokenType.TargetLevel:
                        stack.Push(targetLevel);
                        break;

                    case TokenType.TargetStrength:
                        stack.Push(targetStats[StatKind.Strength]);
                        break;

                    case TokenType.TargetIntelligence:
                        stack.Push(targetStats[StatKind.Intelligence]);
                        break;

                    case TokenType.TargetWisdom:
                        stack.Push(targetStats[StatKind.Wisdom]);
                        break;

                    case TokenType.TargetDexterity:
                        stack.Push(targetStats[StatKind.Dexterity]);
                        break;

                    case TokenType.TargetConstitution:
                        stack.Push(targetStats[StatKind.Constitution]);
                        break;

                    case TokenType.HitDamage: // TODO: how to make distinction between hit damage and effect damage ?
                        stack.Push(effectiveDamageAmount);
                        break;

                    case TokenType.WeaponLevel: // TODO: precompute in put in context ?
                        {
                            if (!CharacterHelpers.TryGetMainHandWeapon(source, out var item, out var _))
                                stack.Push(1); // TODO:
                            else
                            {
                                stack.Push(item.Get<Level>().Value);
                            }
                        }
                        break;

                    case TokenType.WeaponDamage: // TODO: precompute in put in context ?
                        {
                            if (!CharacterHelpers.TryGetMainHandWeapon(source, out var _, out var weapon))
                                stack.Push(1); // TODO:
                            else
                            {
                                int total = 0;
                                for (int i = 0; i < weapon.DiceCount; i++) total += Random.Shared.Next(1, weapon.DiceValue + 1);
                                stack.Push(total);
                            }
                        }
                        break;

                    case TokenType.EffectStackCount:
                        stack.Push(ctx.StackCount);
                        break;

                    case TokenType.Operator:
                        {
                            decimal b = stack.Pop();
                            decimal a = stack.Pop();
                            stack.Push(token.FuncName switch
                            {
                                "+" => a + b,
                                "-" => a - b,
                                "*" => a * b,
                                "/" => a / b,
                                ">" => a > b ? 1m : 0m,
                                "<" => a < b ? 1m : 0m,
                                "=" => a == b ? 1m : 0m,
                                ">=" => a >= b ? 1m : 0m,
                                "<=" => a <= b ? 1m : 0m,
                                "==" => a == b ? 1m : 0m,
                                "!=" => a != b ? 1m : 0m,
                                _ => throw new Exception($"Unknown operator {token.FuncName}")
                            });
                            break;
                        }

                    case TokenType.LogicalOperator:
                        {
                            decimal b = stack.Pop();
                            decimal a = stack.Pop();
                            stack.Push(token.FuncName switch
                            {
                                "&&" => (a != 0 && b != 0) ? 1m : 0m,
                                "||" => (a != 0 || b != 0) ? 1m : 0m,
                                _ => throw new Exception($"Unknown logical operator {token.FuncName}")
                            });
                        }
                        break;

                    case TokenType.Function:
                        if (!_functions.TryGetValue(token.FuncName, out var func))
                            throw new Exception($"Unknown function {token.FuncName}");

                        if (stack.Count < token.FuncArgCount)
                            throw new Exception($"Not enough arguments for function {token.FuncName}");

                        var args = new decimal[token.FuncArgCount];
                        for (int i = token.FuncArgCount - 1; i >= 0; i--)
                            args[i] = stack.Pop(); // maintain left-to-right order

                        stack.Push(func(args));
                        break;
                }
            }

            return stack.Pop();
        };
    }

    // Tokens
    enum TokenType
    {
        Number,
        Operator,
        LogicalOperator,
        LeftParen,
        RightParen,
        Function,
        Comma,
        // Caster
        CasterLevel,
        CasterStrength,
        CasterIntelligence,
        CasterWisdom,
        CasterDexterity,
        CasterConstitution,
        // Target
        TargetLevel,
        TargetStrength,
        TargetIntelligence,
        TargetWisdom,
        TargetDexterity,
        TargetConstitution,
        // Weapon
        WeaponLevel,
        WeaponDamage,
        // Damage
        HitDamage,
        // Effect
        EffectStackCount,
    }

    struct Token
    {
        public required TokenType Type;
        public decimal Number;
        public string FuncName;
        public int FuncArgCount;
        public required int StartIndex; // used for error highlighting
    };

    // Tokenizer
    //  Turns:
    //  3 + caster.Level / 2 - range(1, 4)
    //  into tokens.
    static List<Token> Tokenize(string expr)
    {
        var lastTokenWasArithmeticOperator = true; // for unary minus detection
        var tokens = new List<Token>();

        int i = 0;

        while (i < expr.Length)
        {
            if (char.IsWhiteSpace(expr[i]))
            {
                i++;
                continue;
            }

            int start = i; // track start index for errors

            // Number (decimal)
            if (char.IsDigit(expr[i]) || expr[i] == '.')
            {
                bool hasDot = false;

                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                {
                    if (expr[i] == '.')
                    {
                        if (hasDot) throw TokenError(expr, i, "Invalid number format");
                        hasDot = true;
                    }
                    i++;
                }

                if (!decimal.TryParse(expr[start..i], NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    throw TokenError(expr, start, "Invalid number format");

                tokens.Add(new Token
                {
                    Type = TokenType.Number,
                    Number = number,
                    StartIndex = start
                });

                lastTokenWasArithmeticOperator = false;
                continue;
            }

            // Logical operators
            if (expr[i] == '&' && i + 1 < expr.Length && expr[i + 1] == '&')
            {
                tokens.Add(new Token { Type = TokenType.LogicalOperator, FuncName = "&&", StartIndex = start });
                i += 2;
                lastTokenWasArithmeticOperator = false;
                continue;
            }

            if (expr[i] == '|' && i + 1 < expr.Length && expr[i + 1] == '|')
            {
                tokens.Add(new Token { Type = TokenType.LogicalOperator, FuncName = "||", StartIndex = start });
                i += 2;
                lastTokenWasArithmeticOperator = false;
                continue;
            }

            // Arithmetic
            if (expr[i] == '+' || expr[i] == '-')
            {
                if (expr[i] == '-' && lastTokenWasArithmeticOperator) // unary minus
                {
                    tokens.Add(new Token { Type = TokenType.Function, FuncName = "neg", FuncArgCount = 1, StartIndex = start });
                    i++;
                }
                else
                {
                    tokens.Add(new Token { Type = TokenType.Operator, FuncName = expr[i++].ToString(), StartIndex = start });
                }
                lastTokenWasArithmeticOperator = true;
                continue;
            }

            if (expr[i] == '*' || expr[i] == '/')
            {
                tokens.Add(new Token { Type = TokenType.Operator, FuncName = expr[i++].ToString(), StartIndex = start });
                lastTokenWasArithmeticOperator = true;
                continue;
            }

            if (expr[i] == '(')
            {
                tokens.Add(new Token { Type = TokenType.LeftParen, StartIndex = start });
                i++;
                lastTokenWasArithmeticOperator = true;
                continue;
            }

            if (expr[i] == ')')
            {
                tokens.Add(new Token { Type = TokenType.RightParen, StartIndex = start });
                i++;
                lastTokenWasArithmeticOperator = false;
                continue;
            }

            // !=
            if (expr[i] == '!')
            {
                if (i + 1 < expr.Length && expr[i + 1] == '=')
                {
                    tokens.Add(new Token { Type = TokenType.Operator, FuncName = "!=", StartIndex = start });
                    i += 2;
                }
                else
                    throw TokenError(expr, i, "Unexpected '!' operator. Did you mean '!='?");

                lastTokenWasArithmeticOperator = false;
                continue;
            }

            // Multi-char comparisons
            if (i + 1 < expr.Length)
            {
                string twoChar = expr.Substring(i, 2);
                switch (twoChar)
                {
                    case ">=": tokens.Add(new Token { Type = TokenType.Operator, FuncName = ">=", StartIndex = start }); i += 2; lastTokenWasArithmeticOperator = false; continue;
                    case "<=": tokens.Add(new Token { Type = TokenType.Operator, FuncName = "<=", StartIndex = start }); i += 2; lastTokenWasArithmeticOperator = false; continue;
                    case "==": tokens.Add(new Token { Type = TokenType.Operator, FuncName = "==", StartIndex = start }); i += 2; lastTokenWasArithmeticOperator = false; continue;
                    //case "!=": tokens.Add(new Token { Type = TokenType.Operator, FuncName = "!=" }); i += 2; lastTokenWasArithmeticOperator = false; continue; handled in previous case
                }
            }

            // Single-char comparisons
            if (expr[i] == '>')
            { 
                tokens.Add(new Token { Type = TokenType.Operator, FuncName = ">", StartIndex = start });
                i++;
                lastTokenWasArithmeticOperator = false;
                continue;
            }
            if (expr[i] == '<') {
                tokens.Add(new Token { Type = TokenType.Operator, FuncName = "<", StartIndex = start });
                i++;
                lastTokenWasArithmeticOperator = false;
                continue;
            }

            // Comma
            if (expr[i] == ',')
            {
                tokens.Add(new Token { Type = TokenType.Comma, StartIndex = start });
                i++;
                lastTokenWasArithmeticOperator = true;
                continue;
            }

            // Identifier
            if (char.IsLetter(expr[i]))
            {
                while (i < expr.Length && (char.IsLetter(expr[i]) || expr[i] == '.')) i++;
                string tokenStr = expr[start..i].ToLowerInvariant();

                switch (tokenStr)
                {
                    // Caster
                    case "caster.level": tokens.Add(new Token { Type = TokenType.CasterLevel, StartIndex = start }); break;
                    case "caster.strength": tokens.Add(new Token { Type = TokenType.CasterStrength, StartIndex = start }); break;
                    case "caster.intelligence": tokens.Add(new Token { Type = TokenType.CasterIntelligence, StartIndex = start }); break;
                    case "caster.wisdom": tokens.Add(new Token { Type = TokenType.CasterWisdom, StartIndex = start }); break;
                    case "caster.dexterity": tokens.Add(new Token { Type = TokenType.CasterDexterity, StartIndex = start }); break;
                    case "caster.constitution": tokens.Add(new Token { Type = TokenType.CasterConstitution, StartIndex = start }); break;

                    // Target
                    case "target.level": tokens.Add(new Token { Type = TokenType.TargetLevel, StartIndex = start }); break;
                    case "target.strength": tokens.Add(new Token { Type = TokenType.TargetStrength, StartIndex = start }); break;
                    case "target.intelligence": tokens.Add(new Token { Type = TokenType.TargetIntelligence, StartIndex = start }); break;
                    case "target.wisdom": tokens.Add(new Token { Type = TokenType.TargetWisdom, StartIndex = start }); break;
                    case "target.dexterity": tokens.Add(new Token { Type = TokenType.TargetDexterity, StartIndex = start }); break;
                    case "target.constitution": tokens.Add(new Token { Type = TokenType.TargetConstitution, StartIndex = start }); break;

                    // Weapon
                    case "weapon.level": tokens.Add(new Token { Type = TokenType.WeaponLevel, StartIndex = start }); break;
                    case "weapon.damage": tokens.Add(new Token { Type = TokenType.WeaponDamage, StartIndex = start }); break;

                    // Damage
                    case "hitdamage": tokens.Add(new Token { Type = TokenType.HitDamage, StartIndex = start }); break;

                    // Effect
                    case "effect.stackcount": tokens.Add(new Token { Type = TokenType.EffectStackCount, StartIndex = start }); break;

                    default:
                        // function detection
                        if (i < expr.Length && expr[i] == '(')
                            tokens.Add(new Token { Type = TokenType.Function, FuncName = tokenStr, StartIndex = start });
                        else
                            throw TokenError(expr, start, $"Unknown identifier '{tokenStr}'");
                        break;
                }
                lastTokenWasArithmeticOperator = false;
                continue;
            }

            throw TokenError(expr, i, $"Unexpected character '{expr[i]}'");
        }

        return tokens;
    }

    static Exception TokenError(string formula, int pos, string message)
    {
        // limit snippet length
        int snippetStart = Math.Max(0, pos - 10);
        int snippetEnd = Math.Min(formula.Length, pos + 10);
        string snippet = formula[snippetStart..snippetEnd];

        // caret position in snippet
        int caretPos = pos - snippetStart;

        return new Exception($"{message} at position {pos}:\n{snippet}\n{new string(' ', caretPos)}^");
    }

    // RPN Conversion(Shunting Yard)
    //  Converts math to Reverse Polish Notation for fast evaluation.
    static int Prec(Token t)
    {
        if (t.Type == TokenType.Function && t.FuncName == "neg")
            return 6; // highest precedence
        if (t.Type == TokenType.Operator)
        {
            return t.FuncName switch
            {
                "*" or "/" => 5,
                "+" or "-" => 4,
                ">" or "<" or ">=" or "<=" or "==" or "!=" => 3,
                _ => 0
            };
        }
        if (t.Type == TokenType.LogicalOperator)
        {
            return t.FuncName switch
            {
                "&&" => 2,
                "||" => 1,
                _ => 0
            };
        }
        return 0;
    }

    static List<Token> ToRpn(List<Token> tokens)
    {
        var output = new List<Token>();
        var ops = new Stack<Token>();

        // Track argument counts PER FUNCTION
        var argCount = new Stack<int>();
        var isFunction = new Stack<bool>();

        foreach (var t in tokens)
        {
            switch (t.Type)
            {
                case TokenType.Number:
                case TokenType.CasterLevel:
                case TokenType.CasterStrength:
                case TokenType.CasterIntelligence:
                case TokenType.CasterWisdom:
                case TokenType.CasterDexterity:
                case TokenType.CasterConstitution:
                case TokenType.TargetLevel:
                case TokenType.TargetStrength:
                case TokenType.TargetIntelligence:
                case TokenType.TargetWisdom:
                case TokenType.TargetDexterity:
                case TokenType.TargetConstitution:
                case TokenType.WeaponLevel:
                case TokenType.WeaponDamage:
                case TokenType.HitDamage:
                case TokenType.EffectStackCount:
                    output.Add(t);
                    break;

                case TokenType.Function:
                    ops.Push(t);
                    isFunction.Push(true);
                    break;

                case TokenType.LeftParen:
                    ops.Push(t);

                    // If this '(' belongs to a function → start arg counting
                    if (ops.Count >= 2 && ops.ElementAt(1).Type == TokenType.Function)
                    {
                        argCount.Push(0);
                    }
                    else
                    {
                        isFunction.Push(false);
                    }
                    break;

                case TokenType.Comma:
                    while (ops.Count > 0 && ops.Peek().Type != TokenType.LeftParen)
                        output.Add(ops.Pop());

                    // Only count comma if inside function
                    if (argCount.Count > 0)
                    {
                        argCount.Push(argCount.Pop() + 1);
                    }
                    break;

                case TokenType.RightParen:
                    while (ops.Count > 0 && ops.Peek().Type != TokenType.LeftParen)
                        output.Add(ops.Pop());

                    if (ops.Count == 0)
                        throw new Exception("Mismatched parentheses");

                    ops.Pop(); // remove '('

                    // If function before '(' → finalize it
                    if (ops.Count > 0 && ops.Peek().Type == TokenType.Function)
                    {
                        var func = ops.Pop();

                        int count = argCount.Pop();

                        func.FuncArgCount = count + 1; // commas + 1

                        output.Add(func);
                    }
                    break;

                case TokenType.Operator:
                case TokenType.LogicalOperator:
                    while (ops.Count > 0 &&
                           (ops.Peek().Type == TokenType.Operator ||
                            ops.Peek().Type == TokenType.LogicalOperator ||
                            ops.Peek().Type == TokenType.Function) &&
                           Prec(ops.Peek()) >= Prec(t))
                    {
                        output.Add(ops.Pop());
                    }
                    ops.Push(t);
                    break;
            }
        }

        while (ops.Count > 0)
        {
            if (ops.Peek().Type == TokenType.LeftParen)
                throw new Exception("Mismatched parentheses");

            output.Add(ops.Pop());
        }

        return output;
    }
}