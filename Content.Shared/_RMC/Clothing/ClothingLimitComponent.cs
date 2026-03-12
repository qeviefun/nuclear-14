// #Misfits Add - RMC clothing limit component ported from RMC-14 (MIT)
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class ClothingLimitComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slot = SlotFlags.EARS;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Id;
}
