// #Misfits Add - RMC magnetize item event ported from RMC-14 (MIT)
using Content.Shared.Inventory;

namespace Content.Shared._RMC.Armor.Magnetic;

[ByRefEvent]
public record struct RMCMagnetizeItemEvent(
    EntityUid User,
    EntityUid Item,
    SlotFlags MagnetizeToSlots,
    SlotFlags TargetSlots,
    EntityUid? Magnetizer = null,
    EntityUid? ReceivingItem = null,
    string ReceivingContainer = ""
) : IInventoryRelayEvent;
