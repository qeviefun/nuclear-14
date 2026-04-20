// #Misfits Add - Tracks the player who purchased a pet companion perk so the
// spawned ghost-role pet appears next to that player's current location instead
// of the original character spawn point.

namespace Content.Server._Misfits.Pets;

/// <summary>
/// Attached to invisible pet ghost-role spawner entities created by
/// <c>TraitSpawnEntity</c>. Stores the perk-owning player so that when a ghost
/// takes the role, the pet is moved to the owner's current coordinates rather
/// than the stale spawner position.
/// </summary>
[RegisterComponent]
public sealed partial class MisfitsPetSpawnerOwnerComponent : Component
{
    /// <summary>
    /// The player entity that purchased the pet perk and should receive the
    /// companion next to them.
    /// </summary>
    [DataField]
    public EntityUid Owner;
}
