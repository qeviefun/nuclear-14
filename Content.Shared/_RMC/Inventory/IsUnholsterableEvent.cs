// #Misfits Add - RMC unholsterable event ported from RMC-14 (MIT)
namespace Content.Shared._RMC.Inventory;

[ByRefEvent]
public record struct IsUnholsterableEvent(bool Unholsterable);
