// #Misfits Add - RMC attachable altered event stub ported from RMC-14 (MIT)
// Stub: only the types needed by the magnetic attachment system
namespace Content.Shared._RMC.Attachable.Events;

public enum AttachableAlteredType
{
    Attached,
    Detached,
}

[ByRefEvent]
public readonly record struct AttachableAlteredEvent(EntityUid Holder, AttachableAlteredType Alteration);
