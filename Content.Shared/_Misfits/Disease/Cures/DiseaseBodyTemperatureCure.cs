// #Misfits Fix - Moved from Content.Server to Content.Shared so client can resolve
// the type during DiseasePrototype YAML deserialization.
// Cure() is a no-op here; DiseaseSystem handles body temperature cure logic
// directly because TemperatureComponent is server-only.

namespace Content.Shared._Misfits.Disease.Cures;

/// <summary>
/// Disease is cured when the entity's body temperature crosses a threshold.
/// Can check for high temperature (fever breaking) or low temperature (cooling down).
/// </summary>
public sealed partial class DiseaseBodyTemperatureCure : DiseaseCure
{
    /// <summary>Temperature threshold in Kelvin.</summary>
    [DataField]
    public float Threshold { get; private set; } = 310f;

    /// <summary>If true, cure when temperature is ABOVE threshold. If false, cure when BELOW.</summary>
    [DataField]
    public bool MustBeAbove { get; private set; } = true;

    // Cure() intentionally not overridden — DiseaseSystem handles temperature check
    // directly because TemperatureComponent is server-only.
}
