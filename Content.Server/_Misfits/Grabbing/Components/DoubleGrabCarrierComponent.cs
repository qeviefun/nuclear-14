// #Misfits Change /Add/ - Tracks an escalated second grab that transitions into a choking carry.
namespace Content.Server._Misfits.Grabbing.Components;

[RegisterComponent]
public sealed partial class DoubleGrabCarrierComponent : Component
{
    [DataField]
    public EntityUid Victim = default!;

    [DataField]
    public TimeSpan HeldTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan SuffocationStartTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan CritTime = TimeSpan.FromSeconds(30);

    [DataField]
    public float CarrySpeedModifier = 0.3f;

    [DataField]
    public float SuffocationDrainPerSecond = 0.5f;

    [DataField]
    public bool CritApplied;
}