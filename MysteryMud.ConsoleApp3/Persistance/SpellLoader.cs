using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Data;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Formulas;

namespace MysteryMud.ConsoleApp3.Persistance;

public static class SpellLoader
{
    public static SpellDatabase LoadSpells(SpellJsonRoot doc)
    {
        var formulaCompiler = new FormulaCompiler();

        // load templates first so spells can reference them
        var templates = new Dictionary<string, EffectTemplate>();
        foreach (var e in doc.Effects)
        {
            var template = new EffectTemplate
            {
                Name = e.Name,
                Tag = Enum.Parse<EffectTagId>(e.Tag),
                Stacking = Enum.Parse<StackingRule>(e.Stacking),
                MaxStacks = e.MaxStacks,
                //TODO: Flags = Enum.Parse<AffectFlags>(e.Flags),
                StatModifiers = e.StatModifiers.ConvertAll(sm => new StatModifierDefinition
                {
                    Stat = Enum.Parse<StatType>(sm.Stat),
                    Type = Enum.Parse<ModifierType>(sm.Type),
                    Value = sm.Value
                }).ToArray(),
                ApplyMessage = e.ApplyMessage,
                WearOffMessage = e.WearOffMessage
            };

            // Dynamic formulas evaluated at cast time
            if (e.DurationFormula != null)
                template.DurationFunc = formulaCompiler.Compile(e.DurationFormula);

            if (e.Dot != null && e.Dot.DamageFormula != null)
            {
                template.Dot = new DotDefinition
                {
                    DamageFunc = formulaCompiler.Compile(e.Dot.DamageFormula),
                    DamageType = Enum.Parse<DamageType>(e.Dot.DamageType),
                    TickRate = e.Dot.TickRate,
                };
            }

            if (e.Hot != null && e.Hot.HealFormula != null)
            {
                template.Hot = new HotDefinition
                {
                    HealFunc = formulaCompiler.Compile(e.Hot.HealFormula),
                    TickRate = e.Hot.TickRate,
                };
            }

            templates[e.Name] = template;
        }

        // load spells
        var spells = new Dictionary<string, SpellDefinition>();
        foreach (var s in doc.Spells)
        {
            spells[s.Name] = new SpellDefinition
            {
                Name = s.Name,
                Effects = s.Effects.ConvertAll(name => templates[name]).ToArray()
            };
        }

        var spellDatabase = new SpellDatabase
        {
            EffectTemplates = templates,
            Spells = spells
        };

        return spellDatabase;
    }
}
