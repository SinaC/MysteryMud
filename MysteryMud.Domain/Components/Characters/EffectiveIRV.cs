namespace MysteryMud.Domain.Components.Characters;

public struct EffectiveIRV
{
    public ulong Immunities; // bitfield of DamageKind
    public ulong Resistances; // bitfield of DamageKind
    public ulong Vulnerabilities; // bitfield of DamageKind
}
