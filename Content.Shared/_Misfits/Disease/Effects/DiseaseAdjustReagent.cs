// #Misfits Fix - Moved from Content.Server to Content.Shared so client can resolve
// the type during DiseasePrototype YAML deserialization.
// Effect() is a no-op here; DiseaseSystem handles reagent logic directly because
// BloodstreamComponent is server-only.

using Content.Shared.FixedPoint;

namespace Content.Shared._Misfits.Disease.Effects;

/// <summary>
/// Injects or removes a reagent from the infected entity's bloodstream.
/// Used for diseases that induce toxins or deplete beneficial chemicals.
/// </summary>
public sealed partial class DiseaseAdjustReagent : DiseaseEffect
{
    /// <summary>Reagent prototype ID to inject.</summary>
    [DataField(required: true)]
    public string Reagent { get; private set; } = string.Empty;

    /// <summary>Amount to inject per tick. Negative values remove the reagent.</summary>
    [DataField]
    public float Amount { get; private set; } = 3f;

    // Effect() intentionally not overridden — DiseaseSystem handles reagent adjustment
    // directly because BloodstreamComponent is server-only.
}
