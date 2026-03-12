// #Misfits Add - RMC webbing component ported from RMC-14 (MIT)
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Rsi? PlayerSprite;

    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
