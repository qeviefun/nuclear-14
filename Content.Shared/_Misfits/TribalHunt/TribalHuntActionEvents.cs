using Content.Shared.Actions;

namespace Content.Shared._Misfits.TribalHunt;

/// <summary>
/// Raised when the tribal elder starts a new hunt contract.
/// </summary>
public sealed partial class PerformTribalStartHuntActionEvent : InstantActionEvent;

/// <summary>
/// Raised when a tribal participant offers a trophy to the active hunt.
/// </summary>
public sealed partial class PerformTribalOfferTrophyActionEvent : InstantActionEvent;
