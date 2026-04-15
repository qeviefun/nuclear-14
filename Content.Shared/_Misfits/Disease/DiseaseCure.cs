// #Misfits Add - Abstract base for disease cures.
// Cures are checked per tick. When a cure's condition is met, the disease is removed.

namespace Content.Shared._Misfits.Disease;

/// <summary>
/// Base class for disease cure conditions. Subclasses implement specific checks
/// (reagent in bloodstream, bedrest, time elapsed, body temperature, etc.).
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class DiseaseCure
{
    /// <summary>Which stages this cure is available during (0-indexed).</summary>
    [DataField]
    public List<int> Stages { get; private set; } = new() { 0 };

    /// <summary>
    /// Check whether the cure condition is met.
    /// Virtual (not abstract) so concrete types can live in Shared for client-side
    /// type resolution even when their implementation is server-only.
    /// </summary>
    /// <returns>True if the disease should be cured.</returns>
    public virtual bool Cure(DiseaseEffectArgs args) => false;
}
