// #Misfits Add - RMC item slots visuals ported from RMC-14 (MIT)
using Robust.Shared.Serialization;

namespace Content.Shared._RMC.Inventory;

[Serializable, NetSerializable]
public enum CMItemSlotsVisuals
{
    Empty,
    Low,
    Medium,
    High,
    Full,
}
