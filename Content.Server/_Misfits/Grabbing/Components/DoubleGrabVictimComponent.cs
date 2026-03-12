// #Misfits Change /Add/ - Marks a carried victim as being in the double-grab choke state.
namespace Content.Server._Misfits.Grabbing.Components;

[RegisterComponent]
public sealed partial class DoubleGrabVictimComponent : Component
{
    [DataField]
    public EntityUid Carrier = default!;

    [DataField]
    public TimeSpan EscapeTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan NextGaspEmoteTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan GaspEmoteCooldown = TimeSpan.FromSeconds(4);
}