using Content.Shared._Misfits.TribalHunt;
using Content.Shared.Destructible;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Misfits.TribalHunt;

/// <summary>
/// Handles spawning, tracking, and loot drops for legendary creatures during tribal hunts.
/// </summary>
public sealed partial class LegendaryCreatureSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LegendaryCreatureComponent, DestructionEventArgs>(OnCreatureDestroyed);
    }

    /// <summary>
    /// Spawns a legendary creature at a random point on the target map.
    /// </summary>
    public EntityUid? TrySpawnLegendaryCreature(string creatureProto, EntityUid huntSessionId, MapId mapId)
    {
        if (!_mapManager.MapExists(mapId))
            return null;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        var spawnCoords = new EntityCoordinates(mapUid, _random.NextVector2(100f, 500f));

        var creature = Spawn(creatureProto, spawnCoords);

        if (TryComp<LegendaryCreatureComponent>(creature, out var legComp))
        {
            legComp.HuntSessionId = huntSessionId;
            Dirty(creature, legComp);
        }

        return creature;
    }

    private void OnCreatureDestroyed(EntityUid uid, LegendaryCreatureComponent comp, DestructionEventArgs args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        for (int i = 0; i < comp.LeatherDropCount; i++)
        {
            Spawn("TribalLegendaryLeather", xform.Coordinates);
        }
    }
}
