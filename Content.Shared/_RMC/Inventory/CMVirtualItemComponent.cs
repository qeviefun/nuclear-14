// #Misfits Add - RMC virtual item component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Inventory;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMVirtualItemComponent : Component;
