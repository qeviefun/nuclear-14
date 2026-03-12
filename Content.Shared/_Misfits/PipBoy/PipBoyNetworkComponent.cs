// #Misfits Add - PipBoy Network component.
// Adds contacts, groups, directory visibility, password lock, status, and SOS alerts to NanoChatCard entities.
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.PipBoy;

/// <summary>
/// Adds PipBoy networking features to a NanoChatCard entity (ID card inserted in a Pip-Boy).
/// Added dynamically by <see cref="SharedPipBoyNetworkSystem"/> on MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PipBoyNetworkComponent : Component
{
    /// <summary>Whether this PipBoy appears in the directory/masterlist to other PipBoys.</summary>
    [DataField, AutoNetworkedField]
    public bool IsVisible = true;

    /// <summary>
    /// Password string for locking (stored server-side, never sent to client).
    /// Null means no password is set.
    /// </summary>
    [DataField]
    public string? Password;

    /// <summary>Whether the PipBoy Hub is currently locked (requires password to access).</summary>
    [DataField, AutoNetworkedField]
    public bool IsLocked;

    /// <summary>Contacts keyed by their NanoChat/PipBoy number.</summary>
    [DataField]
    public Dictionary<uint, PipBoyContact> Contacts = new();

    /// <summary>Group memberships keyed by group ID.</summary>
    [DataField]
    public Dictionary<uint, PipBoyGroupMembership> Groups = new();

    /// <summary>Presence status shown to other PipBoy users.</summary>
    [DataField]
    public PipBoyPresenceStatus PresenceStatus = PipBoyPresenceStatus.Available;

    /// <summary>Custom status message shown alongside presence status.</summary>
    [DataField]
    public string? StatusMessage;

    /// <summary>Received SOS alerts from contacts. Cleared when viewed.</summary>
    [DataField]
    public List<PipBoySosAlert> SosAlerts = new();
}
