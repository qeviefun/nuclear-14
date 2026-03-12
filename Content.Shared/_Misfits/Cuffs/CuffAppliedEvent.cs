// #Misfits Add: public events raised at each stage of cuffing/uncuffing so server systems can react (e.g. chat logging).
namespace Content.Shared._Misfits.Cuffs;

/// <summary>
/// Raised on the target entity after they are successfully restrained via handcuffs.
/// </summary>
[ByRefEvent]
public readonly record struct CuffAppliedEvent(EntityUid User, EntityUid Target);

/// <summary>
/// Raised on the target entity when a cuffing do-after is started (i.e. the user begins trying to cuff).
/// </summary>
[ByRefEvent]
public readonly record struct CuffStartedEvent(EntityUid User, EntityUid Target);

/// <summary>
/// Raised on the target entity when an uncuffing do-after is started (i.e. the user begins trying to remove cuffs).
/// </summary>
[ByRefEvent]
public readonly record struct UncuffStartedEvent(EntityUid User, EntityUid Target);
