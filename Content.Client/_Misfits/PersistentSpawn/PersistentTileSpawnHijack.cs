// #Misfits Add - PlacementHijack for the Persistent Tile Spawn system.
// Intercepts tile placement requests and sends PersistentTileSpawnRequestEvent
// so the server system can persist them to JSON.
using Content.Shared._Misfits.PersistentSpawn;
using Robust.Client.Placement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Misfits.PersistentSpawn;

/// <summary>
/// Hijacks the tile placement to send <see cref="PersistentTileSpawnRequestEvent"/>
/// instead of the normal MsgPlacement for tiles.
/// </summary>
public sealed class PersistentTileSpawnHijack : PlacementHijack
{
    private readonly IEntityManager _entityManager;
    private readonly IEntityNetworkManager _entityNetworkManager;
    private readonly SharedTransformSystem _transformSystem;
    private readonly ITileDefinitionManager _tileDefs;

    public PersistentTileSpawnHijack(
        IEntityManager entityManager,
        IEntityNetworkManager entityNetworkManager,
        SharedTransformSystem transformSystem,
        ITileDefinitionManager tileDefs)
    {
        _entityManager = entityManager;
        _entityNetworkManager = entityNetworkManager;
        _transformSystem = transformSystem;
        _tileDefs = tileDefs;
    }

    public override bool HijackPlacementRequest(EntityCoordinates coordinates)
    {
        if (Manager.CurrentPermission == null)
            return false;

        if (!Manager.CurrentPermission.IsTile)
            return false;

        var tileType = Manager.CurrentPermission.TileType;
        var tileDef = _tileDefs[tileType];

        // Convert to map coordinates for persistence
        var mapCoords = _transformSystem.ToMapCoordinates(coordinates);

        // Build the rotation/mirror byte the same way the engine does
        var dirByte = Tile.DirectionToByte(Manager.Direction);
        byte rotMirror = (byte)(dirByte + (Manager.Mirrored ? 4 : 0));

        var msg = new PersistentTileSpawnRequestEvent(
            tileDef.ID,
            mapCoords.Position.X,
            mapCoords.Position.Y,
            rotMirror);

        _entityNetworkManager.SendSystemNetworkMessage(msg);

        return true; // We handled the placement
    }

    public override bool HijackDeletion(EntityUid entity)
    {
        // Tiles aren't entities — let the engine handle entity deletion normally
        return false;
    }

    public override bool HijackDeletion(EntityCoordinates coordinates)
    {
        return false;
    }
}
