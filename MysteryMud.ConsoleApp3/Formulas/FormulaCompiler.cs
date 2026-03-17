using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Formulas;

public class FormulaCompiler
{
    // Formula Compiler
    //  +  -  *  /
    //  caster.Level
    //  caster.Strength
    //  target.Level
    //  target.Strength
    //  numbers
    //  parentheses
    public Func<World, Entity, Entity, int> Compile(string formula)
    {
        var rpn = ToRpn(Tokenize(formula));

        return (world, caster, target) =>
        {
            var stack = new Stack<int>();

            ref var casterStats = ref caster.Get<EffectiveStats>();
            ref var targetStats = ref target.Get<EffectiveStats>();

            foreach (var token in rpn)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        stack.Push(token.Number);
                        break;

                    case TokenType.CasterLevel:
                        stack.Push(casterStats.Level);
                        break;

                    case TokenType.CasterStrength:
                        stack.Push(casterStats.Values[StatType.Strength]);
                        break;

                    case TokenType.CasterIntelligence:
                        stack.Push(casterStats.Values[StatType.Intelligence]);
                        break;

                    case TokenType.CasterWisdom:
                        stack.Push(casterStats.Values[StatType.Wisdom]);
                        break;

                    case TokenType.CasterDexterity:
                        stack.Push(casterStats.Values[StatType.Dexterity]);
                        break;

                    case TokenType.CasterConsitution:
                        stack.Push(casterStats.Values[StatType.Constitution]);
                        break;

                    case TokenType.TargetLevel:
                        stack.Push(targetStats.Level);
                        break;

                    case TokenType.TargetStrength:
                        stack.Push(targetStats.Values[StatType.Strength]);
                        break;

                    case TokenType.TargetIntelligence:
                        stack.Push(targetStats.Values[StatType.Intelligence]);
                        break;

                    case TokenType.TargetWisdom:
                        stack.Push(targetStats.Values[StatType.Wisdom]);
                        break;

                    case TokenType.TargetDexterity:
                        stack.Push(targetStats.Values[StatType.Dexterity]);
                        break;

                    case TokenType.TargetConsitution:
                        stack.Push(targetStats.Values[StatType.Constitution]);
                        break;

                    case TokenType.Operator:
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();

                            switch (token.Op)
                            {
                                case '+': stack.Push(a + b); break;
                                case '-': stack.Push(a - b); break;
                                case '*': stack.Push(a * b); break;
                                case '/': stack.Push(a / b); break;
                            }

                            break;
                        }
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
        // Caster
        CasterLevel,
        CasterStrength,
        CasterIntelligence,
        CasterWisdom,
        CasterDexterity,
        CasterConsitution,
        // Target
        TargetLevel,
        TargetStrength,
        TargetIntelligence,
        TargetWisdom,
        TargetDexterity,
        TargetConsitution,
    }

    struct Token
    {
        public TokenType Type;
        public int Number;
        public char Op;
    };

    // Tokenizer
    //  Turns:
    //  3 + caster.Level / 2
    //  into tokens.
    static List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();

        var parts = expr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in parts)
        {
            if (int.TryParse(p, out var n))
            {
                tokens.Add(new Token { Type = TokenType.Number, Number = n });
                continue;
            }

            switch (p)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    tokens.Add(new Token { Type = TokenType.Operator, Op = p[0] });
                    break;

                case "caster.Level":
                    tokens.Add(new Token { Type = TokenType.CasterLevel });
                    break;

                case "caster.Strength":
                    tokens.Add(new Token { Type = TokenType.CasterStrength });
                    break;

                case "caster.Intelligence":
                    tokens.Add(new Token { Type = TokenType.CasterIntelligence });
                    break;

                case "caster.Wisdom":
                    tokens.Add(new Token { Type = TokenType.CasterWisdom });
                    break;

                case "caster.Dexterity":
                    tokens.Add(new Token { Type = TokenType.CasterDexterity });
                    break;

                case "caster.Constitution":
                    tokens.Add(new Token { Type = TokenType.CasterConsitution });
                    break;

                case "target.Level":
                    tokens.Add(new Token { Type = TokenType.TargetLevel });
                    break;

                case "target.Strength":
                    tokens.Add(new Token { Type = TokenType.TargetStrength });
                    break;

                case "target.Intelligence":
                    tokens.Add(new Token { Type = TokenType.TargetIntelligence });
                    break;

                case "target.Wisdom":
                    tokens.Add(new Token { Type = TokenType.TargetWisdom });
                    break;

                case "target.Dexterity":
                    tokens.Add(new Token { Type = TokenType.TargetDexterity });
                    break;

                case "target.Constitution":
                    tokens.Add(new Token { Type = TokenType.TargetConsitution });
                    break;

                default:
                    throw new Exception($"Unknown token {p}");
            }
        }

        return tokens;
    }

    // RPN Conversion(Shunting Yard)
    //  Converts math to Reverse Polish Notation for fast evaluation.
    static int Prec(char op) => op switch
    {
        '*' or '/' => 2,
        '+' or '-' => 1,
        _ => 0
    };

    static List<Token> ToRpn(List<Token> tokens)
    {
        var output = new List<Token>();
        var ops = new Stack<Token>();

        foreach (var t in tokens)
        {
            if (t.Type != TokenType.Operator)
            {
                output.Add(t);
                continue;
            }

            while (ops.Count > 0 && Prec(ops.Peek().Op) >= Prec(t.Op))
                output.Add(ops.Pop());

            ops.Push(t);
        }

        while (ops.Count > 0)
            output.Add(ops.Pop());

        return output;
    }
}