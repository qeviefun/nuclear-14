// #Misfits Fix - Moved from Content.Server to Content.Shared so client can resolve
// the type during DiseasePrototype YAML deserialization.
// Effect() is a no-op; actual sneeze/cough logic handled by DiseaseSystem.HandleSnough().

namespace Content.Shared._Misfits.Disease.Effects;

/// <summary>
/// Forces the entity to sneeze or cough via the emote system.
/// If the disease is airborne and the entity has no mask, nearby
/// DiseaseCarrier entities within range may be infected.
/// </summary>
public sealed partial class DiseaseSnough : DiseaseEffect
{
    /// <summary>Emote prototype ID to trigger ("Sneeze" or "Cough").</summary>
    [DataField]
    public string EmoteId { get; private set; } = "Cough";

    /// <summary>Whether this snough can spread the disease to nearby entities.</summary>
    [DataField]
    public bool AirTransmit { get; private set; } = true;

    // Effect() intentionally not overridden — DiseaseSystem handles snough logic
    // directly because it needs server-only system dependencies (ChatSystem, EntityLookup).
}
