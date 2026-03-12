// #Misfits Add - PipBoy Hub cartridge component (server-side state per cartridge instance).
namespace Content.Server._Misfits.PipBoy;

[RegisterComponent, Access(typeof(PipBoyHubCartridgeSystem))]
public sealed partial class PipBoyHubCartridgeComponent : Component
{
    /// <summary>Cached reference to the NanoChatCard in the containing PDA.</summary>
    [DataField]
    public EntityUid? Card;

    /// <summary>Currently selected group ID for viewing messages/tracking.</summary>
    [DataField]
    public uint? SelectedGroupId;
}
