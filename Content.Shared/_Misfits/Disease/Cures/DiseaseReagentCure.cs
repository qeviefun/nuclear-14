// #Misfits Fix - Moved from Content.Server to Content.Shared so client can resolve
// the type during DiseasePrototype YAML deserialization.
// Cure() is a no-op here; DiseaseSystem handles reagent cure logic
// directly because BloodstreamComponent is server-only.

namespace Content.Shared._Misfits.Disease.Cures;

/// <summary>
/// Disease is cured when the entity has a minimum amount of a specific reagent
/// in their bloodstream chemistry solution.
/// </summary>
public sealed partial class DiseaseReagentCure : DiseaseCure
{
    /// <summary>Reagent prototype ID to check for.</summary>
    [DataField(required: true)]
    public string Reagent { get; private set; } = string.Empty;

    /// <summary>Minimum amount of reagent required in bloodstream.</summary>
    [DataField]
    public float MinAmount { get; private set; } = 5f;

    // Cure() intentionally not overridden — DiseaseSystem handles reagent check
    // directly because BloodstreamComponent is server-only.
}
