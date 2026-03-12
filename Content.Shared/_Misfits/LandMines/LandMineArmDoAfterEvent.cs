// #Misfits Add - DoAfter event for landmine arm interaction (paired with LandMineDisarmDoAfterEvent)
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.LandMines;

[Serializable, NetSerializable]
public sealed partial class LandMineArmDoAfterEvent : SimpleDoAfterEvent
{
}
