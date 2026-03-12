// #Misfits Add - Network messages for the Persistent Entity Spawn system.
// Entities spawned via this system survive round restarts via JSON persistence.
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.PersistentSpawn;

/// <summary>
/// Client → Server: request to spawn a persistent entity at the given world position.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersistentSpawnRequestEvent : EntityEventArgs
{
    /// <summary>Prototype ID of the entity to spawn.</summary>
    public string PrototypeId { get; }

    /// <summary>World X coordinate on the map.</summary>
    public float X { get; }

    /// <summary>World Y coordinate on the map.</summary>
    public float Y { get; }

    /// <summary>Rotation in radians.</summary>
    public double Rotation { get; }

    public PersistentSpawnRequestEvent(string prototypeId, float x, float y, double rotation)
    {
        PrototypeId = prototypeId;
        X = x;
        Y = y;
        Rotation = rotation;
    }
}

/// <summary>
/// Client → Server: request to erase a persistent entity by its network entity ID.
/// Also used from the regular Entity Spawn Panel erase mode.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersistentEraseRequestEvent : EntityEventArgs
{
    public NetEntity Target { get; }

    public PersistentEraseRequestEvent(NetEntity target)
    {
        Target = target;
    }
}

/// <summary>
/// Client → Server: request to place a persistent tile at the given world position.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersistentTileSpawnRequestEvent : EntityEventArgs
{
    /// <summary>Tile definition name (string ID, not numeric TileId).</summary>
    public string TileDefName { get; }

    /// <summary>World X coordinate.</summary>
    public float X { get; }

    /// <summary>World Y coordinate.</summary>
    public float Y { get; }

    /// <summary>Tile rotation/mirror byte used by the engine Tile struct.</summary>
    public byte RotationMirroring { get; }

    public PersistentTileSpawnRequestEvent(string tileDefName, float x, float y, byte rotationMirroring)
    {
        TileDefName = tileDefName;
        X = x;
        Y = y;
        RotationMirroring = rotationMirroring;
    }
}

// #Misfits Add - Persistent Decal Spawn network messages.
// Decals placed via the Persistent Decal Spawn panel are stored in JSON and re-applied every round start.

/// <summary>
/// Client → Server: request to place a persistent decal at the given world position.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersistentDecalSpawnRequestEvent : EntityEventArgs
{
    /// <summary>DecalPrototype ID (e.g. "Caution").</summary>
    public string DecalId { get; }

    /// <summary>World X coordinate on the grid.</summary>
    public float X { get; }

    /// <summary>World Y coordinate on the grid.</summary>
    public float Y { get; }

    /// <summary>Rotation in degrees.</summary>
    public float Rotation { get; }

    /// <summary>RGBA color packed as ARGB int for net serialization.</summary>
    public int ColorArgb { get; }

    /// <summary>Whether to snap the decal to tile center.</summary>
    public bool Snap { get; }

    /// <summary>Z-draw order index.</summary>
    public int ZIndex { get; }

    /// <summary>Whether the decal can be cleaned by mops/cleaning tools.</summary>
    public bool Cleanable { get; }

    public PersistentDecalSpawnRequestEvent(string decalId, float x, float y, float rotation, int colorArgb, bool snap, int zIndex, bool cleanable)
    {
        DecalId = decalId;
        X = x;
        Y = y;
        Rotation = rotation;
        ColorArgb = colorArgb;
        Snap = snap;
        ZIndex = zIndex;
        Cleanable = cleanable;
    }
}

/// <summary>
/// Client → Server: request to erase all persistent decals near a world position (right-click erase).
/// </summary>
[Serializable, NetSerializable]
public sealed class PersistentDecalEraseRequestEvent : EntityEventArgs
{
    /// <summary>World X coordinate of the erase location.</summary>
    public float X { get; }

    /// <summary>World Y coordinate of the erase location.</summary>
    public float Y { get; }

    public PersistentDecalEraseRequestEvent(float x, float y)
    {
        X = x;
        Y = y;
    }
}
