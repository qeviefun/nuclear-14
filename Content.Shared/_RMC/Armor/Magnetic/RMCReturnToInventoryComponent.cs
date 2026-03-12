// #Misfits Add - RMC return to inventory component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Armor.Magnetic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCReturnToInventoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid User;

    [DataField, AutoNetworkedField]
    public EntityUid Magnetizer;

    [DataField, AutoNetworkedField]
    public bool Returned;

    [DataField, AutoNetworkedField]
    public EntityUid? ReceivingItem;

    [DataField, AutoNetworkedField]
    public string ReceivingContainer = string.Empty;
}
