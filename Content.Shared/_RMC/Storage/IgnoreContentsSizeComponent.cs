// #Misfits Add - RMC ignore contents size component ported from RMC-14 (MIT)
// Allows specific oversized items to bypass size checks when inserted into this storage.
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IgnoreContentsSizeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Items = new();
}
