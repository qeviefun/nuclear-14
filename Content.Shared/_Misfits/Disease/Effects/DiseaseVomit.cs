// #Misfits Fix - Moved from Content.Server to Content.Shared so client can resolve
// the type during DiseasePrototype YAML deserialization.
// Effect() is a no-op here; DiseaseSystem handles vomit logic directly because
// VomitSystem is server-only.

namespace Content.Shared._Misfits.Disease.Effects;

/// <summary>
/// Triggers the VomitSystem on the diseased entity, causing them to vomit
/// (hunger/thirst loss, stun, puddle creation).
/// </summary>
public sealed partial class DiseaseVomit : DiseaseEffect
{
    /// <summary>Thirst penalty from vomiting.</summary>
    [DataField]
    public float ThirstAmount { get; private set; } = -40f;

    /// <summary>Hunger penalty from vomiting.</summary>
    [DataField]
    public float HungerAmount { get; private set; } = -40f;

    // Effect() intentionally not overridden — DiseaseSystem handles vomit logic
    // directly because VomitSystem is server-only.
}
