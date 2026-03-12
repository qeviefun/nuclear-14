// #Misfits Add - RMC limited storage component ported from RMC-14 (MIT)
// Enforces per-category item count limits in storage (e.g. max 1 gun, max 6 mags)
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageLimitSystem))]
public sealed partial class LimitedStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Limit> Limits = new();

    [DataDefinition]
    [Serializable, NetSerializable]
    public partial struct Limit()
    {
        [DataField]
        public int Count = 1;

        [DataField]
        public EntityWhitelist? Blacklist = new();

        [DataField(required: true)]
        public EntityWhitelist? Whitelist = new();

        [DataField(required: true)]
        public LocId Popup;
    }
}
