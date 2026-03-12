// #Misfits Add - RMC attachable magnetic component ported from RMC-14 (MIT)
using Content.Shared._RMC.Attachable.Systems;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableMagneticSystem))]
public sealed partial class AttachableMagneticComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags MagnetizeToSlots = SlotFlags.SUITSTORAGE;
}
