// #Misfits Add - RMC clothing require equipped component ported from RMC-14 (MIT)
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class ClothingRequireEquippedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public string DenyReason = "rmc-wear-required-item";

    [DataField, AutoNetworkedField]
    public bool AutoUnequip = false;
}
