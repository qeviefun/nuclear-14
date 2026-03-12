// #Misfits Add - RMC clothing block webbing ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Webbing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class ClothingBlockWebbingComponent : Component;
