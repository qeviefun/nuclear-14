// #Misfits Add - RMC dropped event ported from RMC-14 (MIT)
namespace Content.Shared._RMC.Inventory;

[ByRefEvent]
public readonly record struct RMCDroppedEvent(EntityUid User);
