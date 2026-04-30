using Robust.Shared.Serialization;
using Content.Shared.Eui;

namespace Content.Shared._Misfits.Supporter;

[Serializable, NetSerializable]
public sealed class SupporterEntry
{
    public Guid UserId;
    public string Username = string.Empty;
    public string? Title;
    public string? NameColor;

    public SupporterEntry() { }

    public SupporterEntry(Guid userId, string username, string? title, string? nameColor)
    {
        UserId = userId;
        Username = username;
        Title = title;
        NameColor = nameColor;
    }
}

[Serializable, NetSerializable]
public sealed class SupporterManagerState : EuiStateBase
{
    public readonly List<SupporterEntry> Supporters;
    public readonly string? StatusMessage;

    public SupporterManagerState(List<SupporterEntry> supporters, string? statusMessage = null)
    {
        Supporters = supporters;
        StatusMessage = statusMessage;
    }
}

/// <summary>
/// Set or update a supporter. If UserId is provided it is used directly; otherwise the server
/// resolves the GUID from Username.
/// </summary>
[Serializable, NetSerializable]
public sealed class SupporterSetMessage : EuiMessageBase
{
    public Guid? UserId;
    public string Username = string.Empty;
    public string? Title;
    public string? NameColor;
}

[Serializable, NetSerializable]
public sealed class SupporterRemoveMessage : EuiMessageBase
{
    public Guid UserId;
}
