namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record WeaponProcData
(
    string Name,
    int Chance, // TODO: formula
    // TODO: message ?
    List<WeaponProcEffectData> Effects
);
