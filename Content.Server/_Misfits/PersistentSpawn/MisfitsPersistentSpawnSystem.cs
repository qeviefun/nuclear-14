// #Misfits Add - Server system for the Persistent Entity Spawn, Tile Spawn, and Decal Spawn features.
// Entities, tiles, and decals placed through the Persistent Spawn Menus are saved to
// JSON files and automatically re-spawned/replaced every round start.
// Erasing a persistent entity (from either spawn panel) removes its JSON entry.
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Administration.Managers;
using Content.Server.Decals;
using Content.Server.GameTicking;
using Content.Shared._Misfits.PersistentSpawn;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Placement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.PersistentSpawn;

/// <summary>
/// Manages the lifecycle of persistent entities and tiles:
/// spawn on admin request, persist to JSON, re-spawn on round start, remove on erase.
/// </summary>
public sealed class MisfitsPersistentSpawnSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    // #Misfits Add - DecalSystem for placing/removing decals server-side
    [Dependency] private readonly DecalSystem _decalSystem = default!;

    private ISawmill _log = default!;

    private const string EntityDataFileName = "persistent_entities.json";
    private const string TileDataFileName = "persistent_tiles.json";
    // #Misfits Add - decal persistence file
    private const string DecalDataFileName = "persistent_decals.json";
    private string _entitySaveFilePath = string.Empty;
    private string _tileSaveFilePath = string.Empty;
    private string _decalSaveFilePath = string.Empty;

    /// <summary>All persistent entity records, keyed by unique persistence ID.</summary>
    private readonly Dictionary<string, PersistentEntityRecord> _entityRecords = new();

    /// <summary>All persistent tile records, keyed by unique persistence ID.</summary>
    private readonly Dictionary<string, PersistentTileRecord> _tileRecords = new();

    // #Misfits Add - all persistent decal records, keyed by unique persistence ID
    private readonly Dictionary<string, PersistentDecalRecord> _decalRecords = new();

    /// <summary>
    /// Reverse lookup: spawned EntityUid → persistence ID.
    /// Only valid for entities alive this round.
    /// </summary>
    private readonly Dictionary<EntityUid, string> _uidToPersistenceId = new();

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("persistent_spawn");

        var userDataPath = _resourceManager.UserData.RootDir ?? ".";
        _entitySaveFilePath = Path.Combine(userDataPath, EntityDataFileName);
        _tileSaveFilePath = Path.Combine(userDataPath, TileDataFileName);
        // #Misfits Add - decal persistence file path
        _decalSaveFilePath = Path.Combine(userDataPath, DecalDataFileName);

        LoadEntityRecords();
        LoadTileRecords();
        // #Misfits Add - load persistent decal records
        LoadDecalRecords();

        // Listen for round start to re-spawn all persistent entities, tiles, and decals
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);

        // Listen for network requests from admin clients
        SubscribeNetworkEvent<PersistentSpawnRequestEvent>(OnSpawnRequest);
        SubscribeNetworkEvent<PersistentEraseRequestEvent>(OnEraseRequest);
        SubscribeNetworkEvent<PersistentTileSpawnRequestEvent>(OnTileSpawnRequest);
        // #Misfits Add - decal spawn/erase network subscriptions
        SubscribeNetworkEvent<PersistentDecalSpawnRequestEvent>(OnDecalSpawnRequest);
        SubscribeNetworkEvent<PersistentDecalEraseRequestEvent>(OnDecalEraseRequest);

        // Catch erase from the standard Entity Spawn Panel (engine placement manager)
        SubscribeLocalEvent<PlacementEntityEvent>(OnPlacementEntityEvent);
    }

    // ── Round start: re-spawn all saved entities ───────────────────────────────

    private void OnRoundStarted(RoundStartedEvent args)
    {
        _uidToPersistenceId.Clear();
        LoadEntityRecords();
        LoadTileRecords();

        // Find the first non-nullspace map (the game map that was loaded).
        MapId? targetMap = null;
        foreach (var mapId in _mapManager.GetAllMapIds())
        {
            if (mapId == MapId.Nullspace)
                continue;
            targetMap = mapId;
            break;
        }

        if (targetMap == null)
        {
            _log.Warning("No valid map found for persistent respawn.");
            return;
        }

        // ── Respawn persistent entities ────────────────────────────────────────
        var spawned = 0;
        foreach (var (id, record) in _entityRecords)
        {
            if (!_prototypeManager.HasIndex<EntityPrototype>(record.PrototypeId))
            {
                _log.Warning($"Persistent entity prototype '{record.PrototypeId}' no longer exists — skipping (ID {id}).");
                continue;
            }

            var coords = new MapCoordinates(new Vector2(record.X, record.Y), targetMap.Value);
            var rotation = Angle.FromDegrees(record.RotationDegrees);

            var uid = EntityManager.SpawnEntity(record.PrototypeId, coords);
            _transformSystem.SetWorldRotation(uid, rotation);

            var comp = EnsureComp<MisfitsPersistentEntityComponent>(uid);
            comp.PersistenceId = id;

            _uidToPersistenceId[uid] = id;
            spawned++;
        }

        _log.Info($"Respawned {spawned} persistent entities from {EntityDataFileName}.");

        // ── Respawn persistent tiles ───────────────────────────────────────────
        var tilesPlaced = 0;
        foreach (var (id, tileRec) in _tileRecords)
        {
            if (!_tileDefinitionManager.TryGetDefinition(tileRec.TileDefName, out var tileDef))
            {
                _log.Warning($"Persistent tile def '{tileRec.TileDefName}' no longer exists — skipping (ID {id}).");
                continue;
            }

            var mapCoords = new MapCoordinates(new Vector2(tileRec.X, tileRec.Y), targetMap.Value);
            if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
                continue;

            var tilePos = _mapSystem.WorldToTile(gridUid, grid, mapCoords.Position);
            _mapSystem.SetTile(gridUid, grid, tilePos, new Tile(tileDef.TileId, rotationMirroring: tileRec.RotationMirroring));
            tilesPlaced++;
        }

        _log.Info($"Restored {tilesPlaced} persistent tiles from {TileDataFileName}.");

        // ── Restore persistent decals ──────────────────────────────────────────
        var decalsPlaced = 0;
        foreach (var (id, rec) in _decalRecords)
        {
            if (!_prototypeManager.HasIndex<DecalPrototype>(rec.DecalId))
            {
                _log.Warning($"Persistent decal prototype '{rec.DecalId}' no longer exists — skipping (ID {id}).");
                continue;
            }

            var mapCoords = new MapCoordinates(new Vector2(rec.X, rec.Y), targetMap.Value);
            if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
                continue;

            // Convert world → local grid position for EntityCoordinates
            var invMatrix = _transformSystem.GetInvWorldMatrix(gridUid);
            var localPos = Vector2.Transform(mapCoords.Position, invMatrix);
            var entityCoords = new EntityCoordinates(gridUid, localPos);

            // Unpack ARGB int back to Color
            var argb = (uint) rec.ColorArgb;
            var color = new Color(
                ((argb >> 16) & 0xFF) / 255f,
                ((argb >> 8) & 0xFF) / 255f,
                (argb & 0xFF) / 255f,
                ((argb >> 24) & 0xFF) / 255f);

            _decalSystem.TryAddDecal(rec.DecalId, entityCoords, out _, color, Angle.FromDegrees(rec.Rotation), rec.ZIndex, rec.Cleanable);
            decalsPlaced++;
        }

        _log.Info($"Restored {decalsPlaced} persistent decals from {DecalDataFileName}.");
    }

    // ── Spawn request from Persistent Entity Spawn Menu ────────────────────────

    private void OnSpawnRequest(PersistentSpawnRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Server))
        {
            _log.Warning($"{session.Name} lacks Server flag for persistent entity spawn.");
            return;
        }

        if (!_prototypeManager.HasIndex<EntityPrototype>(msg.PrototypeId))
        {
            _log.Warning($"Invalid prototype '{msg.PrototypeId}' in persistent spawn request from {session.Name}.");
            return;
        }

        // Find the map the admin is on
        var adminEntity = session.AttachedEntity;
        if (adminEntity == null)
            return;

        var mapId = Transform(adminEntity.Value).MapID;
        var coords = new MapCoordinates(new Vector2(msg.X, msg.Y), mapId);
        var rotation = new Angle(msg.Rotation);

        var uid = EntityManager.SpawnEntity(msg.PrototypeId, coords);
        _transformSystem.SetWorldRotation(uid, rotation);

        // Generate a unique persistence ID and tag the entity
        var persistenceId = Guid.NewGuid().ToString();
        var comp = EnsureComp<MisfitsPersistentEntityComponent>(uid);
        comp.PersistenceId = persistenceId;

        _uidToPersistenceId[uid] = persistenceId;

        // Save the record
        _entityRecords[persistenceId] = new PersistentEntityRecord
        {
            PrototypeId = msg.PrototypeId,
            X = msg.X,
            Y = msg.Y,
            RotationDegrees = rotation.Degrees,
            SpawnedBy = session.Name,
        };

        PersistEntityRecords();
        _log.Info($"Persistent entity '{msg.PrototypeId}' spawned by {session.Name} at ({msg.X:F1}, {msg.Y:F1}). ID: {persistenceId}");
    }

    // ── Tile spawn request from Persistent Tile Spawn Menu ─────────────────────

    private void OnTileSpawnRequest(PersistentTileSpawnRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Server))
        {
            _log.Warning($"{session.Name} lacks Server flag for persistent tile spawn.");
            return;
        }

        if (!_tileDefinitionManager.TryGetDefinition(msg.TileDefName, out var tileDef))
        {
            _log.Warning($"Invalid tile def '{msg.TileDefName}' in persistent tile spawn request from {session.Name}.");
            return;
        }

        var adminEntity = session.AttachedEntity;
        if (adminEntity == null)
            return;

        var mapId = Transform(adminEntity.Value).MapID;
        var mapCoords = new MapCoordinates(new Vector2(msg.X, msg.Y), mapId);

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
        {
            _log.Warning($"No grid found at ({msg.X:F1}, {msg.Y:F1}) for persistent tile spawn by {session.Name}.");
            return;
        }

        var tilePos = _mapSystem.WorldToTile(gridUid, grid, mapCoords.Position);
        _mapSystem.SetTile(gridUid, grid, tilePos, new Tile(tileDef.TileId, rotationMirroring: msg.RotationMirroring));

        // Remove any existing persistent tile records at this grid position (deduplication + erase support).
        var toRemove = _tileRecords
            .Where(kvp =>
            {
                var recMapCoords = new MapCoordinates(new Vector2(kvp.Value.X, kvp.Value.Y), mapId);
                if (!_mapManager.TryFindGridAt(recMapCoords, out var recGridUid, out var recGrid))
                    return false;
                if (recGridUid != gridUid)
                    return false;
                return _mapSystem.WorldToTile(recGridUid, recGrid, recMapCoords.Position) == tilePos;
            })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in toRemove)
            _tileRecords.Remove(id);

        // If we're placing "Space" (tile 0), that's an erase — don't create a new record.
        if (tileDef.TileId == 0)
        {
            if (toRemove.Count > 0)
            {
                PersistTileRecords();
                _log.Info($"Erased {toRemove.Count} persistent tile record(s) at ({msg.X:F1}, {msg.Y:F1}) by {session.Name}.");
            }
            return;
        }

        var persistenceId = Guid.NewGuid().ToString();
        _tileRecords[persistenceId] = new PersistentTileRecord
        {
            TileDefName = msg.TileDefName,
            X = msg.X,
            Y = msg.Y,
            RotationMirroring = msg.RotationMirroring,
            SpawnedBy = session.Name,
        };

        PersistTileRecords();
        _log.Info($"Persistent tile '{msg.TileDefName}' placed by {session.Name} at ({msg.X:F1}, {msg.Y:F1}). ID: {persistenceId}");
    }

    // ── Decal spawn request from Persistent Decal Spawn Menu ─────────────────────

    private void OnDecalSpawnRequest(PersistentDecalSpawnRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Server))
        {
            _log.Warning($"{session.Name} lacks Server flag for persistent decal spawn.");
            return;
        }

        if (!_prototypeManager.HasIndex<DecalPrototype>(msg.DecalId))
        {
            _log.Warning($"Invalid decal prototype '{msg.DecalId}' in persistent decal spawn request from {session.Name}.");
            return;
        }

        var adminEntity = session.AttachedEntity;
        if (adminEntity == null)
            return;

        var mapId = Transform(adminEntity.Value).MapID;
        var mapCoords = new MapCoordinates(new Vector2(msg.X, msg.Y), mapId);

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
        {
            _log.Warning($"No grid found at ({msg.X:F1}, {msg.Y:F1}) for persistent decal spawn by {session.Name}.");
            return;
        }

        // Convert world → local grid position
        var invMatrix = _transformSystem.GetInvWorldMatrix(gridUid);
        var localPos = Vector2.Transform(mapCoords.Position, invMatrix);
        var entityCoords = new EntityCoordinates(gridUid, localPos);

        // Unpack ARGB int → Color
        var argb = (uint) msg.ColorArgb;
        var color = new Color(
            ((argb >> 16) & 0xFF) / 255f,
            ((argb >> 8) & 0xFF) / 255f,
            (argb & 0xFF) / 255f,
            ((argb >> 24) & 0xFF) / 255f);

        if (!_decalSystem.TryAddDecal(msg.DecalId, entityCoords, out _, color, Angle.FromDegrees(msg.Rotation), msg.ZIndex, msg.Cleanable))
        {
            _log.Warning($"Failed to place persistent decal '{msg.DecalId}' at ({msg.X:F1}, {msg.Y:F1}) for {session.Name}.");
            return;
        }

        // Save record
        var persistenceId = Guid.NewGuid().ToString();
        _decalRecords[persistenceId] = new PersistentDecalRecord
        {
            DecalId = msg.DecalId,
            X = msg.X,
            Y = msg.Y,
            Rotation = msg.Rotation,
            ColorArgb = msg.ColorArgb,
            ZIndex = msg.ZIndex,
            Cleanable = msg.Cleanable,
            SpawnedBy = session.Name,
        };

        PersistDecalRecords();
        _log.Info($"Persistent decal '{msg.DecalId}' placed by {session.Name} at ({msg.X:F1}, {msg.Y:F1}). ID: {persistenceId}");
    }

    // ── Decal erase request from Persistent Decal Spawn Menu ───────────────────

    private void OnDecalEraseRequest(PersistentDecalEraseRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Server))
            return;

        var adminEntity = session.AttachedEntity;
        if (adminEntity == null)
            return;

        var mapId = Transform(adminEntity.Value).MapID;
        var mapCoords = new MapCoordinates(new Vector2(msg.X, msg.Y), mapId);

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
            return;

        // Convert world → local grid position for GetDecalsInRange
        var invMatrix = _transformSystem.GetInvWorldMatrix(gridUid);
        var localPos = Vector2.Transform(mapCoords.Position, invMatrix);

        // Remove all live decals in the area (matches vanilla right-click erase behavior)
        foreach (var (decalId, _) in _decalSystem.GetDecalsInRange(gridUid, localPos))
            _decalSystem.RemoveDecal(gridUid, decalId);

        // Remove any persistent records whose world position falls within the erase radius
        var erasePos = new Vector2(msg.X, msg.Y);
        var toRemove = _decalRecords
            .Where(kvp => Vector2.Distance(new Vector2(kvp.Value.X, kvp.Value.Y), erasePos) < 0.75f)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in toRemove)
            _decalRecords.Remove(id);

        if (toRemove.Count > 0)
        {
            PersistDecalRecords();
            _log.Info($"Erased {toRemove.Count} persistent decal record(s) near ({msg.X:F1}, {msg.Y:F1}) by {session.Name}.");
        }
    }

    // ── Erase request from Persistent Entity Spawn Menu ────────────────────────

    private void OnEraseRequest(PersistentEraseRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Server))
            return;

        var uid = EntityManager.GetEntity(msg.Target);
        if (!EntityManager.EntityExists(uid))
            return;

        RemovePersistentRecord(uid);
        EntityManager.DeleteEntity(uid);
    }

    // ── Standard erase from the engine's PlacementManager ──────────────────────

    private void OnPlacementEntityEvent(PlacementEntityEvent args)
    {
        if (args.PlacementEventAction != PlacementEventAction.Erase)
            return;

        // If the entity being erased is a persistent entity, remove its record
        RemovePersistentRecord(args.EditedEntity);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void RemovePersistentRecord(EntityUid uid)
    {
        // Check by component first (works for current-round entities)
        if (TryComp<MisfitsPersistentEntityComponent>(uid, out var comp) &&
            !string.IsNullOrEmpty(comp.PersistenceId))
        {
            if (_entityRecords.Remove(comp.PersistenceId))
            {
                _uidToPersistenceId.Remove(uid);
                PersistEntityRecords();
                _log.Info($"Removed persistent entity record '{comp.PersistenceId}'.");
            }
            return;
        }

        // Fallback: check the UID map (shouldn't normally be needed)
        if (_uidToPersistenceId.TryGetValue(uid, out var id))
        {
            _entityRecords.Remove(id);
            _uidToPersistenceId.Remove(uid);
            PersistEntityRecords();
            _log.Info($"Removed persistent entity record '{id}' (fallback lookup).");
        }
    }

    // ── JSON persistence — entities ────────────────────────────────────────────

    private void LoadEntityRecords()
    {
        _entityRecords.Clear();

        try
        {
            if (!File.Exists(_entitySaveFilePath))
                return;

            var json = File.ReadAllText(_entitySaveFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, PersistentEntityRecord>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
                _entityRecords[kvp.Key] = kvp.Value;

            _log.Debug($"Loaded {_entityRecords.Count} persistent entity records from {EntityDataFileName}.");
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load {EntityDataFileName}: {ex}");
        }
    }

    private void PersistEntityRecords()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entityRecords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_entitySaveFilePath, json);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save {EntityDataFileName}: {ex}");
        }
    }

    // ── JSON persistence — tiles ───────────────────────────────────────────────

    private void LoadTileRecords()
    {
        _tileRecords.Clear();

        try
        {
            if (!File.Exists(_tileSaveFilePath))
                return;

            var json = File.ReadAllText(_tileSaveFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, PersistentTileRecord>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
                _tileRecords[kvp.Key] = kvp.Value;

            _log.Debug($"Loaded {_tileRecords.Count} persistent tile records from {TileDataFileName}.");
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load {TileDataFileName}: {ex}");
        }
    }

    private void PersistTileRecords()
    {
        try
        {
            var json = JsonSerializer.Serialize(_tileRecords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_tileSaveFilePath, json);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save {TileDataFileName}: {ex}");
        }
    }

    // ── JSON persistence — decals ──────────────────────────────────────────────

    private void LoadDecalRecords()
    {
        _decalRecords.Clear();

        try
        {
            if (!File.Exists(_decalSaveFilePath))
                return;

            var json = File.ReadAllText(_decalSaveFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, PersistentDecalRecord>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
                _decalRecords[kvp.Key] = kvp.Value;

            _log.Debug($"Loaded {_decalRecords.Count} persistent decal records from {DecalDataFileName}.");
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load {DecalDataFileName}: {ex}");
        }
    }

    private void PersistDecalRecords()
    {
        try
        {
            var json = JsonSerializer.Serialize(_decalRecords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_decalSaveFilePath, json);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save {DecalDataFileName}: {ex}");
        }
    }
}

/// <summary>
/// A single persistent entity entry stored in the JSON file.
/// </summary>
public sealed class PersistentEntityRecord
{
    [JsonPropertyName("prototypeId")]
    public string PrototypeId { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("rotationDegrees")]
    public double RotationDegrees { get; set; }

    [JsonPropertyName("spawnedBy")]
    public string SpawnedBy { get; set; } = string.Empty;
}

/// <summary>
/// A single persistent tile entry stored in the JSON file.
/// </summary>
public sealed class PersistentTileRecord
{
    [JsonPropertyName("tileDefName")]
    public string TileDefName { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("rotationMirroring")]
    public byte RotationMirroring { get; set; }

    [JsonPropertyName("spawnedBy")]
    public string SpawnedBy { get; set; } = string.Empty;
}

// #Misfits Add - Persistent decal record stored in JSON.
/// <summary>
/// A single persistent decal entry stored in the JSON file.
/// </summary>
public sealed class PersistentDecalRecord
{
    [JsonPropertyName("decalId")]
    public string DecalId { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    /// <summary>Rotation in degrees.</summary>
    [JsonPropertyName("rotation")]
    public float Rotation { get; set; }

    /// <summary>ARGB color packed as int (matches Color.ToArgb() output).</summary>
    [JsonPropertyName("colorArgb")]
    public int ColorArgb { get; set; }

    [JsonPropertyName("zIndex")]
    public int ZIndex { get; set; }

    [JsonPropertyName("cleanable")]
    public bool Cleanable { get; set; }

    [JsonPropertyName("spawnedBy")]
    public string SpawnedBy { get; set; } = string.Empty;
}
