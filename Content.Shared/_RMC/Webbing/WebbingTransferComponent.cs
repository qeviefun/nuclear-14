// #Misfits Add - RMC webbing transfer component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingTransferComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Clothing;

    [DataField, AutoNetworkedField]
    public TransferType Transfer;

    [DataField, AutoNetworkedField]
    public bool Defer = true;

    public enum TransferType
    {
        ToClothing,
        ToWebbing,
    }
}
