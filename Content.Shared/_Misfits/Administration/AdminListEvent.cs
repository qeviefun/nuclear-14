// #Misfits Change - Shared network event for the /admins command popup
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Administration;

/// <summary>
/// Server-to-client event carrying the list of online admins and mentors
/// for the /admins player command popup.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminListEvent : EntityEventArgs
{
    public List<AdminEntry> Admins = new();
    public List<AdminEntry> Mentors = new();
    public int ConnectedPlayers;
    public int AfkPlayers;
    public int GhostPlayers;

    [Serializable, NetSerializable]
    public sealed class AdminEntry
    {
        public string Name = string.Empty;
        public string? Title;
        public int PermissionCount;
    }
}
