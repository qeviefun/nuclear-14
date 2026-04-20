using Content.Server._Misfits.Pets;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

// #Misfits Add - TraitSpawnEntity: spawns a separate entity at the player's position when the trait activates.
// Used for pet companion traits so each pet gets its own ghost-role spawner entity,
// avoiding the component-duplication limitation of TraitAddComponent.

namespace Content.Server._Misfits.Traits;

/// <summary>
///     Spawns one or more entities at the player's location when the trait is applied.
///     Unlike <c>TraitAddComponent</c>, each entity is independent, so multiple traits
///     can each spawn their own ghost-role spawner without component conflicts.
/// </summary>
[UsedImplicitly]
public sealed partial class TraitSpawnEntity : TraitFunction
{
    /// <summary>
    ///     Prototype IDs to spawn at the player's coordinates.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Prototypes { get; private set; } = new();

    public override void OnPlayerSpawn(
        EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    {
        // Resolve the player's current map coordinates for spawning
        var xform = entityManager.GetComponent<TransformComponent>(uid);
        var coords = xform.Coordinates;

        foreach (var proto in Prototypes)
        {
            var spawned = entityManager.SpawnEntity(proto, coords);

            // Tag pet ghost-role spawners with the owning player so the pet
            // is later relocated to the player's live position when a ghost
            // takes the role (handled by MisfitsPetSpawnerOwnerSystem).
            if (entityManager.HasComponent<GhostRoleMobSpawnerComponent>(spawned))
            {
                var ownerComp = entityManager.EnsureComponent<MisfitsPetSpawnerOwnerComponent>(spawned);
                ownerComp.Owner = uid;
            }
        }
    }
}
