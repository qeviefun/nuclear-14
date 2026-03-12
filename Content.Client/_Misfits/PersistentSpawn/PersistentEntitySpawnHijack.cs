// #Misfits Add - PlacementHijack for the Persistent Entity Spawn system.
// Intercepts placement and deletion requests and sends custom network events
// instead of the standard engine placement messages, so the server system
// can persist them to JSON.
using Content.Shared._Misfits.PersistentSpawn;
using Robust.Client.Placement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Misfits.PersistentSpawn;

/// <summary>
/// Hijacks the placement manager to send <see cref="PersistentSpawnRequestEvent"/>
/// instead of the normal MsgPlacement.
/// </summary>
public sealed class PersistentEntitySpawnHijack : PlacementHijack
{
    private readonly IEntityManager _entityManager;
    private readonly IEntityNetworkManager _entityNetworkManager;
    private readonly SharedTransformSystem _transformSystem;

    public PersistentEntitySpawnHijack(
        IEntityManager entityManager,
        IEntityNetworkManager entityNetworkManager,
        SharedTransformSystem transformSystem)
    {
        _entityManager = entityManager;
        _entityNetworkManager = entityNetworkManager;
        _transformSystem = transformSystem;
    }

    public override bool HijackPlacementRequest(EntityCoordinates coordinates)
    {
        if (Manager.CurrentPermission == null)
            return false;

        var prototypeId = Manager.CurrentPermission.EntityType;
        if (string.IsNullOrEmpty(prototypeId))
            return false;

        // Convert to map coordinates for persistence
        var mapCoords = _transformSystem.ToMapCoordinates(coordinates);
        var rotation = Manager.Direction.ToAngle();

        var msg = new PersistentSpawnRequestEvent(
            prototypeId,
            mapCoords.Position.X,
            mapCoords.Position.Y,
            rotation.Theta);

        _entityNetworkManager.SendSystemNetworkMessage(msg);

        return true; // We handled the placement — don't let the engine send MsgPlacement
    }

    public override bool HijackDeletion(EntityUid entity)
    {
        // Send a persistent erase request so the server can remove the JSON record
        var netEnt = _entityManager.GetNetEntity(entity);
        var msg = new PersistentEraseRequestEvent(netEnt);
        _entityNetworkManager.SendSystemNetworkMessage(msg);

        return true; // We handled deletion
    }

    public override bool HijackDeletion(EntityCoordinates coordinates)
    {
        // Coordinate-based deletion isn't used by our system — fall through to default behavior
        return false;
    }
}
