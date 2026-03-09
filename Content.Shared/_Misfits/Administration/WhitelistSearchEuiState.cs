// #Misfits Change - Shared state and messages for the Whitelist Search EUI
using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Administration;

/// <summary>
/// State sent from server to client for the whitelist search EUI.
/// Contains search results and the currently selected player's whitelists.
/// </summary>
[Serializable, NetSerializable]
public sealed class WhitelistSearchEuiState : EuiStateBase
{
    /// <summary>
    /// Player search results matching the last query.
    /// </summary>
    public List<WhitelistPlayerInfo> SearchResults;

    /// <summary>
    /// The currently selected player's name, or null if none selected.
    /// </summary>
    public string? SelectedPlayerName;

    /// <summary>
    /// The currently selected player's user ID, or null if none selected.
    /// </summary>
    public NetUserId? SelectedPlayerId;

    /// <summary>
    /// The currently selected player's job whitelists, or null if none selected.
    /// </summary>
    public HashSet<ProtoId<JobPrototype>>? Whitelists;

    public WhitelistSearchEuiState(
        List<WhitelistPlayerInfo> searchResults,
        string? selectedPlayerName,
        NetUserId? selectedPlayerId,
        HashSet<ProtoId<JobPrototype>>? whitelists)
    {
        SearchResults = searchResults;
        SelectedPlayerName = selectedPlayerName;
        SelectedPlayerId = selectedPlayerId;
        Whitelists = whitelists;
    }
}

/// <summary>
/// Lightweight player info for search results.
/// </summary>
[Serializable, NetSerializable]
public sealed class WhitelistPlayerInfo
{
    public NetUserId UserId;
    public string UserName;

    public WhitelistPlayerInfo(NetUserId userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }
}

/// <summary>
/// Message from client to server to search for players by partial name.
/// </summary>
[Serializable, NetSerializable]
public sealed class SearchPlayersMessage : EuiMessageBase
{
    public string Query;

    public SearchPlayersMessage(string query)
    {
        Query = query;
    }
}

/// <summary>
/// Message from client to server to select a player and load their whitelists.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectPlayerMessage : EuiMessageBase
{
    public NetUserId PlayerId;

    public SelectPlayerMessage(NetUserId playerId)
    {
        PlayerId = playerId;
    }
}

/// <summary>
/// Message from client to server to set a job whitelist for the selected player.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetWhitelistSearchJobMessage : EuiMessageBase
{
    public ProtoId<JobPrototype> Job;
    public bool Whitelisting;

    public SetWhitelistSearchJobMessage(ProtoId<JobPrototype> job, bool whitelisting)
    {
        Job = job;
        Whitelisting = whitelisting;
    }
}
