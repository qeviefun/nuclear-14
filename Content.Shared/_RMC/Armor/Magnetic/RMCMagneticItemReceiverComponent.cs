// #Misfits Add - RMC magnetic item receiver ported from RMC-14 (MIT)
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Armor.Magnetic;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCMagneticItemReceiverComponent : Component;
