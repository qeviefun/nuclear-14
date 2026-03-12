// #Misfits Add - networked appearance enum for landmine armed/disarmed visual state
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.LandMines;

[NetSerializable, Serializable]
public enum LandMineVisuals
{
    // True = armed (animated blinking sprite), False = disarmed (inactive sprite)
    Armed
}
