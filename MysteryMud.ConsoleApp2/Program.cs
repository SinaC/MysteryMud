using Arch.Core;
using MysteryMud.ConsoleApp2.ECS.Components;
using MysteryMud.ConsoleApp2.ECS.Components.Characters;
using MysteryMud.ConsoleApp2.ECS.Components.Effects;

Console.WriteLine("Hello, World!");

var world = World.Create();


Entity victim = world.Create(
    new BaseStats { Strength = 10, Dexterity = 12, Constitution = 14 }
);
Entity caster = world.Create(
    new BaseStats { Strength = 20, Dexterity = 22, Constitution = 24 }
);


// plague: str malus and dot
Entity plagueRoot = world.Create(
    new EffectRoot
    {
        Source = caster,
        Target = victim,
        SpellId = "plague"
    },
    new EffectDuration { ExpireTick = 60 },
    new EffectPower { Value = 15 }
);
world.Create(
    new EffectParent { Root = plagueRoot },
    new EffectTarget { Target = victim },
    new StatModifier { Stat = StatType.Strength, Amount = -5 }
);
world.Create(
    new EffectParent { Root = plagueRoot },
    new EffectTarget { Target = victim },
    new DamageOverTime
    {
        Damage = 2,
        TickRate = 6,
        NextTick = 6
    }
);

// haste: dex bonus and extra attack
Entity hasteInstance = world.Create(
    new EffectRoot
    {
        Source = caster,
        Target = victim,
        SpellId = "Haste"
    },
    new EffectDuration { ExpireTick = 30 },
    new EffectPower { Value = 10 }
);
world.Create(
    new EffectParent { Root = hasteInstance },
    new EffectTarget { Target = victim },
    new StatModifier { Stat = StatType.Dexterity, Amount = 3 }
);

world.Create(
    new EffectParent { Root = hasteInstance },
    new EffectTarget { Target = victim },
    new ExtraAttack { Amount = 1 }
);