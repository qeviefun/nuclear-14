// #Misfits Add - PipBoy Network System (server).
// Manages group directory, contact logic, location tracking, password validation,
// SOS beacons, waypoints, dead drops, encrypted channels, and status.
using Content.Server.Power.Components;
using Content.Shared._Misfits.PipBoy;
using Content.Shared.Access.Components;
using Content.Shared.DeltaV.NanoChat;
using Content.Shared.Radio.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.PipBoy;

/// <summary>
/// Server-side system that manages the global PipBoy network state:
/// group registry, contact acceptance handshake, directory queries, location gathering,
/// SOS beacons, waypoints, dead drops, encrypted channels, and presence status.
/// </summary>
public sealed class PipBoyNetworkSystem : SharedPipBoyNetworkSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Global group registry (ephemeral, lives for the round).
    private readonly Dictionary<uint, PipBoyGroupData> _groups = new();
    private uint _nextGroupId = 1;

    // Global dead drop registry.
    private readonly Dictionary<uint, PipBoyDeadDrop> _deadDrops = new();
    private uint _nextDeadDropId = 1;

    #region Groups

    /// <summary>Create a new group. Returns the group ID.</summary>
    public uint CreateGroup(string name, uint creatorNumber)
    {
        var id = _nextGroupId++;
        var group = new PipBoyGroupData
        {
            GroupId = id,
            GroupName = name,
            CreatorNumber = creatorNumber,
        };
        group.Members.Add(creatorNumber);
        _groups[id] = group;
        return id;
    }

    /// <summary>Try to add a member to a group. Returns false if wrong password or group not found.</summary>
    public bool JoinGroup(uint groupId, uint memberNumber, string? password = null)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return false;

        // Encrypted channel: check password
        if (group.Password != null && group.Password != password)
            return false;

        return group.Members.Add(memberNumber);
    }

    /// <summary>Remove a member from a group.</summary>
    public void LeaveGroup(uint groupId, uint memberNumber)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return;

        group.Members.Remove(memberNumber);

        // Clean up empty groups
        if (group.Members.Count == 0)
            _groups.Remove(groupId);
    }

    /// <summary>Add a message to a group chat.</summary>
    public void AddGroupMessage(uint groupId, PipBoyGroupMessage message)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return;

        group.Messages.Add(message);

        // Cap history at 200 messages
        if (group.Messages.Count > 200)
            group.Messages.RemoveRange(0, group.Messages.Count - 200);
    }

    /// <summary>Get group data by ID.</summary>
    public PipBoyGroupData? GetGroup(uint groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    /// <summary>Get all groups a member belongs to.</summary>
    public List<PipBoyGroupInfo> GetGroupsForMember(uint memberNumber, Dictionary<uint, PipBoyGroupMembership> memberships)
    {
        var result = new List<PipBoyGroupInfo>();
        foreach (var (gId, membership) in memberships)
        {
            if (_groups.TryGetValue(gId, out var group) && group.Members.Contains(memberNumber))
            {
                result.Add(new PipBoyGroupInfo(gId, group.GroupName, group.Members.Count,
                    membership.MapTrackingEnabled, group.Password != null));
            }
        }
        return result;
    }

    #endregion

    #region Contacts

    /// <summary>
    /// Send a contact request from <paramref name="senderNumber"/> to <paramref name="targetNumber"/>.
    /// Creates PendingOutgoing on sender and PendingIncoming on target.
    /// </summary>
    public bool SendContactRequest(uint senderNumber, uint targetNumber)
    {
        if (senderNumber == targetNumber)
            return false;

        var senderCard = FindCardByNumber(senderNumber);
        var targetCard = FindCardByNumber(targetNumber);
        if (senderCard == null || targetCard == null)
            return false;

        if (!TryComp<PipBoyNetworkComponent>(senderCard.Value, out var senderNet) ||
            !TryComp<PipBoyNetworkComponent>(targetCard.Value, out var targetNet))
            return false;

        // Already contacts or pending?
        if (senderNet.Contacts.ContainsKey(targetNumber))
            return false;

        var senderInfo = GetCardDisplayInfo(senderCard.Value);
        var targetInfo = GetCardDisplayInfo(targetCard.Value);

        senderNet.Contacts[targetNumber] = new PipBoyContact(
            targetNumber, targetInfo.Name, targetInfo.JobTitle, PipBoyContactStatus.PendingOutgoing);
        targetNet.Contacts[senderNumber] = new PipBoyContact(
            senderNumber, senderInfo.Name, senderInfo.JobTitle, PipBoyContactStatus.PendingIncoming);

        Dirty(senderCard.Value, senderNet);
        Dirty(targetCard.Value, targetNet);
        return true;
    }

    /// <summary>
    /// Accept an incoming contact request. Transitions both sides to Accepted.
    /// </summary>
    public bool AcceptContactRequest(uint acceptorNumber, uint requesterNumber)
    {
        var acceptorCard = FindCardByNumber(acceptorNumber);
        var requesterCard = FindCardByNumber(requesterNumber);
        if (acceptorCard == null || requesterCard == null)
            return false;

        if (!TryComp<PipBoyNetworkComponent>(acceptorCard.Value, out var acceptorNet) ||
            !TryComp<PipBoyNetworkComponent>(requesterCard.Value, out var requesterNet))
            return false;

        if (!acceptorNet.Contacts.TryGetValue(requesterNumber, out var inContact) ||
            inContact.Status != PipBoyContactStatus.PendingIncoming)
            return false;

        if (!requesterNet.Contacts.TryGetValue(acceptorNumber, out var outContact) ||
            outContact.Status != PipBoyContactStatus.PendingOutgoing)
            return false;

        acceptorNet.Contacts[requesterNumber] = inContact with { Status = PipBoyContactStatus.Accepted };
        requesterNet.Contacts[acceptorNumber] = outContact with { Status = PipBoyContactStatus.Accepted };

        Dirty(acceptorCard.Value, acceptorNet);
        Dirty(requesterCard.Value, requesterNet);
        return true;
    }

    /// <summary>
    /// Reject or remove a contact from both sides.
    /// </summary>
    public void RemoveContact(uint ownerNumber, uint targetNumber)
    {
        var ownerCard = FindCardByNumber(ownerNumber);
        if (ownerCard != null && TryComp<PipBoyNetworkComponent>(ownerCard.Value, out var ownerNet))
        {
            ownerNet.Contacts.Remove(targetNumber);
            Dirty(ownerCard.Value, ownerNet);
        }

        var targetCard = FindCardByNumber(targetNumber);
        if (targetCard != null && TryComp<PipBoyNetworkComponent>(targetCard.Value, out var targetNet))
        {
            targetNet.Contacts.Remove(ownerNumber);
            Dirty(targetCard.Value, targetNet);
        }
    }

    /// <summary>
    /// Toggle whether we share our location with a specific contact.
    /// Also mirrors the flag on the contact's side.
    /// </summary>
    public void ToggleLocationSharing(uint ownerNumber, uint targetNumber)
    {
        var ownerCard = FindCardByNumber(ownerNumber);
        if (ownerCard == null || !TryComp<PipBoyNetworkComponent>(ownerCard.Value, out var ownerNet))
            return;

        if (!ownerNet.Contacts.TryGetValue(targetNumber, out var contact) ||
            contact.Status != PipBoyContactStatus.Accepted)
            return;

        var newSharing = !contact.SharingLocation;
        ownerNet.Contacts[targetNumber] = contact with { SharingLocation = newSharing };
        Dirty(ownerCard.Value, ownerNet);

        // Mirror on the other side
        var targetCard = FindCardByNumber(targetNumber);
        if (targetCard != null && TryComp<PipBoyNetworkComponent>(targetCard.Value, out var targetNet))
        {
            if (targetNet.Contacts.TryGetValue(ownerNumber, out var mirror))
            {
                targetNet.Contacts[ownerNumber] = mirror with { TheyShareLocation = newSharing };
                Dirty(targetCard.Value, targetNet);
            }
        }
    }

    #endregion

    #region Directory

    /// <summary>Get all visible PipBoy directory entries.</summary>
    public List<PipBoyDirectoryEntry> GetDirectory(uint excludeNumber)
    {
        var result = new List<PipBoyDirectoryEntry>();
        var query = EntityQueryEnumerator<NanoChatCardComponent, PipBoyNetworkComponent>();
        while (query.MoveNext(out var uid, out var card, out var net))
        {
            if (card.Number == null || card.Number == excludeNumber || !net.IsVisible)
                continue;

            var info = GetCardDisplayInfo(uid);
            result.Add(new PipBoyDirectoryEntry(card.Number.Value, info.Name, info.JobTitle,
                net.PresenceStatus, net.StatusMessage));
        }

        return result;
    }

    #endregion

    #region Location Tracking

    /// <summary>
    /// Gather world positions for contacts that share their location with us.
    /// </summary>
    public List<PipBoyTrackedLocation> GetContactLocations(Entity<PipBoyNetworkComponent> ent)
    {
        var locs = new List<PipBoyTrackedLocation>();
        foreach (var (num, contact) in ent.Comp.Contacts)
        {
            if (contact.Status != PipBoyContactStatus.Accepted || !contact.TheyShareLocation)
                continue;

            var cardUid = FindCardByNumber(num);
            if (cardUid == null)
                continue;

            if (!TryComp<NanoChatCardComponent>(cardUid.Value, out var card) || card.PdaUid == null)
                continue;

            var xform = Transform(card.PdaUid.Value);
            var worldPos = xform.WorldPosition;
            locs.Add(new PipBoyTrackedLocation(num, contact.Name, worldPos.X, worldPos.Y));
        }
        return locs;
    }

    /// <summary>
    /// Gather world positions for group members who opt in to map tracking in <paramref name="groupId"/>.
    /// </summary>
    public List<PipBoyTrackedLocation> GetGroupLocations(uint groupId, uint excludeNumber)
    {
        var locs = new List<PipBoyTrackedLocation>();
        if (!_groups.TryGetValue(groupId, out var group))
            return locs;

        foreach (var memberNum in group.Members)
        {
            if (memberNum == excludeNumber)
                continue;

            var cardUid = FindCardByNumber(memberNum);
            if (cardUid == null)
                continue;

            if (!TryComp<PipBoyNetworkComponent>(cardUid.Value, out var net))
                continue;

            // Check if this member opted in to map tracking for this group
            if (!net.Groups.TryGetValue(groupId, out var membership) || !membership.MapTrackingEnabled)
                continue;

            if (!TryComp<NanoChatCardComponent>(cardUid.Value, out var card) || card.PdaUid == null)
                continue;

            var info = GetCardDisplayInfo(cardUid.Value);
            var xform = Transform(card.PdaUid.Value);
            var worldPos = xform.WorldPosition;
            locs.Add(new PipBoyTrackedLocation(memberNum, info.Name, worldPos.X, worldPos.Y));
        }
        return locs;
    }

    #endregion

    #region Password

    /// <summary>Set or clear the PipBoy password.</summary>
    public void SetPassword(Entity<PipBoyNetworkComponent> ent, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ent.Comp.Password = null;
            ent.Comp.IsLocked = false;
        }
        else
        {
            ent.Comp.Password = password;
            ent.Comp.IsLocked = true;
        }
        Dirty(ent);
    }

    /// <summary>Attempt to unlock with a password. Returns true if successful.</summary>
    public bool TryUnlock(Entity<PipBoyNetworkComponent> ent, string attempt)
    {
        if (ent.Comp.Password == null)
            return true;

        if (ent.Comp.Password != attempt)
            return false;

        ent.Comp.IsLocked = false;
        Dirty(ent);
        return true;
    }

    /// <summary>Lock the PipBoy (only if a password is set).</summary>
    public void Lock(Entity<PipBoyNetworkComponent> ent)
    {
        if (ent.Comp.Password == null)
            return;

        ent.Comp.IsLocked = true;
        Dirty(ent);
    }

    #endregion

    #region SOS Beacon

    /// <summary>
    /// Broadcast an SOS beacon to all accepted contacts.
    /// Stores the alert on each contact's PipBoyNetworkComponent.
    /// </summary>
    public void BroadcastSos(uint senderNumber, string senderName, float x, float y)
    {
        var alert = new PipBoySosAlert(senderNumber, senderName, x, y, _timing.CurTime);
        var senderCard = FindCardByNumber(senderNumber);
        if (senderCard == null || !TryComp<PipBoyNetworkComponent>(senderCard.Value, out var senderNet))
            return;

        foreach (var (contactNum, contact) in senderNet.Contacts)
        {
            if (contact.Status != PipBoyContactStatus.Accepted)
                continue;

            var contactCard = FindCardByNumber(contactNum);
            if (contactCard == null || !TryComp<PipBoyNetworkComponent>(contactCard.Value, out var contactNet))
                continue;

            contactNet.SosAlerts.Add(alert);
            // Cap SOS history at 20
            if (contactNet.SosAlerts.Count > 20)
                contactNet.SosAlerts.RemoveRange(0, contactNet.SosAlerts.Count - 20);

            Dirty(contactCard.Value, contactNet);
        }
    }

    #endregion

    #region Group Waypoints

    /// <summary>Add a waypoint to a group at specified position.</summary>
    public bool AddGroupWaypoint(uint groupId, string label, float x, float y, string setByName)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return false;

        // Cap at 20 waypoints per group
        if (group.Waypoints.Count >= 20)
            return false;

        group.Waypoints.Add(new PipBoyWaypoint(label, x, y, setByName));
        return true;
    }

    /// <summary>Remove a waypoint from a group by index.</summary>
    public bool RemoveGroupWaypoint(uint groupId, int index)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return false;

        if (index < 0 || index >= group.Waypoints.Count)
            return false;

        group.Waypoints.RemoveAt(index);
        return true;
    }

    /// <summary>Get waypoints for a group.</summary>
    public List<PipBoyWaypoint> GetGroupWaypoints(uint groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return new List<PipBoyWaypoint>();

        return group.Waypoints;
    }

    #endregion

    #region Contact Notes

    /// <summary>Set personal notes on a contact.</summary>
    public void SetContactNote(uint ownerNumber, uint targetNumber, string? notes)
    {
        var ownerCard = FindCardByNumber(ownerNumber);
        if (ownerCard == null || !TryComp<PipBoyNetworkComponent>(ownerCard.Value, out var ownerNet))
            return;

        if (!ownerNet.Contacts.TryGetValue(targetNumber, out var contact))
            return;

        ownerNet.Contacts[targetNumber] = contact with { Notes = notes };
        Dirty(ownerCard.Value, ownerNet);
    }

    #endregion

    #region Status

    /// <summary>Set presence status and optional status message.</summary>
    public void SetStatus(uint ownerNumber, PipBoyPresenceStatus status, string? statusMessage)
    {
        var card = FindCardByNumber(ownerNumber);
        if (card == null || !TryComp<PipBoyNetworkComponent>(card.Value, out var net))
            return;

        net.PresenceStatus = status;
        net.StatusMessage = statusMessage;
        Dirty(card.Value, net);
    }

    #endregion

    #region Encrypted Channels

    /// <summary>Set or clear group password.</summary>
    public bool SetGroupPassword(uint groupId, uint requesterNumber, string? password)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return false;

        // Only the creator can set/change the password
        if (group.CreatorNumber != requesterNumber)
            return false;

        group.Password = string.IsNullOrWhiteSpace(password) ? null : password;
        return true;
    }

    #endregion

    #region Dead Drops

    /// <summary>Create a dead drop at a world position.</summary>
    public uint CreateDeadDrop(uint senderNumber, string senderName, string content,
        float x, float y, uint? targetNumber = null)
    {
        var id = _nextDeadDropId++;
        var drop = new PipBoyDeadDrop(id, senderNumber, senderName, content, x, y,
            _timing.CurTime, targetNumber);
        _deadDrops[id] = drop;
        return id;
    }

    /// <summary>Collect (read and remove) a dead drop.</summary>
    public PipBoyDeadDrop? CollectDeadDrop(uint dropId, uint collectorNumber)
    {
        if (!_deadDrops.TryGetValue(dropId, out var drop))
            return null;

        // If targeted, only the target can collect
        if (drop.TargetNumber.HasValue && drop.TargetNumber.Value != collectorNumber)
            return null;

        _deadDrops.Remove(dropId);
        return drop;
    }

    /// <summary>Get all dead drops near a world position that are visible to a specific PipBoy number.</summary>
    public List<PipBoyDeadDrop> GetNearbyDeadDrops(float x, float y, uint viewerNumber)
    {
        var result = new List<PipBoyDeadDrop>();
        foreach (var drop in _deadDrops.Values)
        {
            // Must be within pickup range
            var dx = drop.X - x;
            var dy = drop.Y - y;
            if (dx * dx + dy * dy > PipBoyDeadDrop.PickupRange * PipBoyDeadDrop.PickupRange)
                continue;

            // Must be visible to this viewer (public or targeted at them)
            if (drop.TargetNumber.HasValue && drop.TargetNumber.Value != viewerNumber)
                continue;

            // Don't show your own drops
            if (drop.SenderNumber == viewerNumber)
                continue;

            result.Add(drop);
        }
        return result;
    }

    #endregion

    #region Radio Relay

    /// <summary>
    /// Check if the given PDA/loader entity has active telecomm infrastructure.
    /// Used to gate group message delivery.
    /// </summary>
    public bool HasTelecommAccess(EntityUid loaderUid)
    {
        // Check if there's a powered telecomm server on the same station
        var xform = Transform(loaderUid);
        var query = EntityQueryEnumerator<TelecomServerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out _, out _, out var power))
        {
            if (power.Powered)
                return true;
        }
        return false;
    }

    #endregion

    #region Helpers

    /// <summary>Find the entity UID of the NanoChatCard with the given number.</summary>
    public EntityUid? FindCardByNumber(uint number)
    {
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Number == number)
                return uid;
        }
        return null;
    }

    /// <summary>Get display name and job title from an ID card entity.</summary>
    public (string Name, string? JobTitle) GetCardDisplayInfo(EntityUid uid)
    {
        if (TryComp<IdCardComponent>(uid, out var idCard))
            return (idCard.FullName ?? "Unknown", idCard.LocalizedJobTitle);
        return ("Unknown", null);
    }

    #endregion
}
