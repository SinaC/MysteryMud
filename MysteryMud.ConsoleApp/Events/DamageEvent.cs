using Arch.Core;

namespace MysteryMud.ConsoleApp.Events;

struct DamageEvent
{
    public Entity Source;
    public Entity Target;
    public int Amount;
    public bool IsSpell;       // differentiates melee vs spell
    public bool IsCritical;
}

struct HealEvent
{
    public Entity Source;
    public Entity Target;
    public int Amount;
}

struct Shield
{
    public int AbsorbAmount;
    public float Duration;
}

struct Thorns
{
    public int Damage;
}
