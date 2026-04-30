namespace MysteryMud.Domain.Components.Characters;

public struct BaseIRV
{
    public ulong Immunities; // bitfield of DamageKind
    public ulong Resistances; // bitfield of DamageKind
    public ulong Vulnerabilities; // bitfield of DamageKind
}
