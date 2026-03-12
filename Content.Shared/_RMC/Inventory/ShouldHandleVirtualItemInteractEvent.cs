// #Misfits Add - RMC virtual item interact event ported from RMC-14 (MIT)
using Content.Shared.Interaction;

namespace Content.Shared._RMC.Inventory;

[ByRefEvent]
public record struct ShouldHandleVirtualItemInteractEvent(BeforeRangedInteractEvent Event, bool Handle = false);
