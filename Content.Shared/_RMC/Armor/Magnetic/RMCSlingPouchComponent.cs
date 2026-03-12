// #Misfits Add - RMC sling pouch component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Armor.Magnetic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCSlingPouchComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Item;
}
