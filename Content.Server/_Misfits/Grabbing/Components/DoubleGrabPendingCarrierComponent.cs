// #Misfits Change /Add/ - Tracks the wind-up between a second grab and a forced choke carry.
namespace Content.Server._Misfits.Grabbing.Components;

[RegisterComponent]
public sealed partial class DoubleGrabPendingCarrierComponent : Component
{
    [DataField]
    public EntityUid Victim = default!;

    [DataField]
    public TimeSpan HeldTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan PinTime = TimeSpan.FromSeconds(10);
}