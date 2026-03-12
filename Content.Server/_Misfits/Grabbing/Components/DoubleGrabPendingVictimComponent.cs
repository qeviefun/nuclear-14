// #Misfits Change /Add/ - Marks a victim as resisting the second-grab wind-up before the choke carry lands.
namespace Content.Server._Misfits.Grabbing.Components;

[RegisterComponent]
public sealed partial class DoubleGrabPendingVictimComponent : Component
{
    [DataField]
    public EntityUid Carrier = default!;
}