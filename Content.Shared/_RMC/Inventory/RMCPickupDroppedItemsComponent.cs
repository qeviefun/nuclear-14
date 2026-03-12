// #Misfits Add - RMC pickup dropped items component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Inventory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class RMCPickupDroppedItemsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> DroppedItems = new();
}
