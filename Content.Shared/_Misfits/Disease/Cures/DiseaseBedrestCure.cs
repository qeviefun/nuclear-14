// #Misfits Fix - Moved from Content.Server to Content.Shared so client can resolve
// the type during DiseasePrototype YAML deserialization.

using Content.Shared.Buckle.Components;

namespace Content.Shared._Misfits.Disease.Cures;

/// <summary>
/// Disease is cured when the entity is buckled to furniture (bed rest).
/// Represents the Fallout theme of resting to recover from illness.
/// </summary>
public sealed partial class DiseaseBedrestCure : DiseaseCure
{
    public override bool Cure(DiseaseEffectArgs args)
    {
        // Entity must be buckled (strapped to a bed/chair/sleeping bag)
        return args.EntityManager.TryGetComponent<BuckleComponent>(args.DiseasedEntity, out var buckle)
               && buckle.Buckled;
    }
}
