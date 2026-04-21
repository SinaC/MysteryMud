namespace MysteryMud.Domain.Persistence;

/// <summary>Stores the DB row id on an item entity after load/save.</summary>
record struct ItemDbId(long Value);
