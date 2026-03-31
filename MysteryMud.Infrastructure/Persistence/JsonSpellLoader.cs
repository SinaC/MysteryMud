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

        // load effect definitions first so spells can reference them
        var effectDefinitions = new Dictionary<string, EffectDefinition>();
        foreach (var effect in data.Effects)
        {
            var definition = new EffectDefinition
            {
                Id = effect.Name,
                Tag = Enum.Parse<EffectTagId>(effect.Tag),
                Stacking = Enum.Parse<StackingRule>(effect.Stacking, ignoreCase: true),
                MaxStacks = Math.Max(1, effect.MaxStacks),
                TickRate = effect.TickRate,
                TickOnApply = effect.TickOnApply,
                //TODO: Flags = Enum.Parse<AffectFlags>(e.Flags),
                StatModifiers = effect.StatModifiers.Select(sm => new StatModifierDefinition
                {
                    Stat = Enum.Parse<StatKind>(sm.Stat, ignoreCase: true),
                    Kind = Enum.Parse<ModifierKind>(sm.Type, ignoreCase: true),
                    Value = sm.Value
                }).ToArray(),
                ApplyMessage = effect.ApplyMessage,
                WearOffMessage = effect.WearOffMessage
            };

            // Dynamic formulas evaluated at cast time
            if (effect.DurationFormula != null)
                definition.DurationFunc = formulaCompiler.Compile(effect.DurationFormula);

            if (effect.Dot != null && effect.Dot.DamageFormula != null)
            {
                definition.Dot = new DotDefinition
                {
                    DamageFunc = formulaCompiler.Compile(effect.Dot.DamageFormula),
                    DamageKind = Enum.Parse<DamageKind>(effect.Dot.DamageKind, ignoreCase: true),
                };
            }

            if (effect.Hot != null && effect.Hot.HealFormula != null)
            {
                definition.Hot = new HotDefinition
                {
                    HealFunc = formulaCompiler.Compile(effect.Hot.HealFormula),
                };
            }

            effectDefinitions[effect.Name] = definition;
        }

        // load spells
        var spells = new Dictionary<string, SpellDefinition>();
        foreach (var s in data.Spells)
        {
            spells[s.Name] = new SpellDefinition
            {
                Name = s.Name,
                Effects = s.Effects.Select(name => effectDefinitions[name]).ToArray()
            };
        }

        var spellDatabase = new SpellDatabase
        {
            EffectDefinitions = effectDefinitions,
            Spells = spells
        };

        return spellDatabase;
    }
}
