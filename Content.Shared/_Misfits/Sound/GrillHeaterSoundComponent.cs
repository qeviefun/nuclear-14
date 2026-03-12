// #Misfits Change /Add/ - Shared component for grills that need startup audio and active heating ambience.
using Robust.Shared.Audio;

namespace Content.Shared._Misfits.Sound;

/// <summary>
/// Adds sound hooks for grill-like heaters that should play a startup cue and
/// enable ambient looping audio only while actively heating.
/// </summary>
[RegisterComponent]
public sealed partial class GrillHeaterSoundComponent : Component
{
    /// <summary>
    /// Played when the heater transitions from off to an active heating state.
    /// </summary>
    [DataField("startSound", required: true)]
    public SoundSpecifier StartSound = default!;

    /// <summary>
    /// Optional sound played when the heater stops heating.
    /// </summary>
    [DataField("stopSound")]
    public SoundSpecifier? StopSound;

    /// <summary>
    /// Tracks whether the heater was active on the previous update so we only
    /// toggle ambience and play one-shot sounds on transitions.
    /// </summary>
    [ViewVariables]
    public bool LastActive;
}