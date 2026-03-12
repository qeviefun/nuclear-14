// #Misfits Add - RMC holster component ported from RMC-14 (MIT)
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC.Inventory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMHolsterComponent : Component
{
    [DataField]
    public List<EntityUid> Contents = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastEjectAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? Cooldown;

    [DataField, AutoNetworkedField]
    public string? CooldownPopup;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/_RMC/Weapons/Guns/gun_pistol_sheathe.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/_RMC/Weapons/Guns/gun_pistol_draw.ogg");
}
