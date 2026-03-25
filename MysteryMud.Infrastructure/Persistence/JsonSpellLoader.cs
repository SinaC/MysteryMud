using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonSpellLoader
{
    public SpellDatabase LoadSpells(string filePath)
    {
        var formulaCompiler = new FormulaCompiler();

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Command JSON file not found: {filePath}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<SpellAndEffectRootData>(json, options)!;

        // load templates first so spells can reference them
        var templates = new Dictionary<string, EffectTemplate>();
        foreach (var e in data.Effects)
        {
            var template = new EffectTemplate
            {
                Name = e.Name,
                Tag = Enum.Parse<EffectTagId>(e.Tag),
                Stacking = Enum.Parse<StackingRule>(e.Stacking, ignoreCase: true),
                MaxStacks = Math.Max(1, e.MaxStacks),
                //TODO: Flags = Enum.Parse<AffectFlags>(e.Flags),
                StatModifiers = e.StatModifiers.Select(sm => new StatModifierDefinition
                {
                    Stat = Enum.Parse<StatType>(sm.Stat, ignoreCase: true),
                    Type = Enum.Parse<ModifierType>(sm.Type, ignoreCase: true),
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
                    DamageType = Enum.Parse<DamageType>(e.Dot.DamageType, ignoreCase: true),
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
        foreach (var s in data.Spells)
        {
            spells[s.Name] = new SpellDefinition
            {
                Name = s.Name,
                Effects = s.Effects.Select(name => templates[name]).ToArray()
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
