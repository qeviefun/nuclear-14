using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.TribalHunt;

/// <summary>
/// Full tribal hunt UI state snapshot sent from the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class TribalHuntUiState
{
    public bool Active;
    public int Offered;
    public int Required;
    public int SecondsRemaining;
    public string StatusText = string.Empty;
    public string CoordinatesText = string.Empty;
    public bool CoordinatesKnown;
}

/// <summary>
/// Server -> client tribal hunt UI update event.
/// </summary>
[Serializable, NetSerializable]
public sealed class TribalHuntUiUpdateEvent : EntityEventArgs
{
    public TribalHuntUiState State = new();
}