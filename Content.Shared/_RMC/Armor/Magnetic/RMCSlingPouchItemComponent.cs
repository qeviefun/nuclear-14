// #Misfits Add - RMC sling pouch item component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Armor.Magnetic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCSlingPouchItemComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityUid Pouch;
}
