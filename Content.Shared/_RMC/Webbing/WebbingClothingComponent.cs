// #Misfits Add - RMC webbing clothing component ported from RMC-14 (MIT)
using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Container = "cm_clothing_webbing_slot";

    [DataField, AutoNetworkedField]
    public EntityUid? Webbing;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? UnequippedSize;

    [DataField, AutoNetworkedField]
    public EntProtoId<WebbingComponent>? StartingWebbing;
}
