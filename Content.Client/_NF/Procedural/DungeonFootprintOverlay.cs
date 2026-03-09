using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._NF.Procedural;

/// <summary>
/// Draws a tinted bounding-box ghost at the cursor showing the approximate
/// maximum footprint of the selected dungeon config.
/// </summary>
public sealed class DungeonFootprintOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly PlacementManager _manager;
    private readonly IPrototypeManager _prototypes;
    private readonly IEyeManager _eye;
    private readonly IEntityManager _entityManager;

    // Estimated half-extents in tiles; built once in constructor.
    private readonly Vector2 _halfExtent;

    private static readonly Color FillColor     = new(0.1f, 0.6f, 1f, 0.15f);
    private static readonly Color BorderColor   = new(0.1f, 0.6f, 1f, 0.80f);
    private static readonly Color OriginColor   = new(1f, 0.85f, 0.1f, 0.90f);

    public DungeonFootprintOverlay(PlacementManager manager, DungeonConfigPrototype config)
    {
        ZIndex = 200;
        _manager = manager;
        _prototypes = IoCManager.Resolve<IPrototypeManager>();
        _eye = IoCManager.Resolve<IEyeManager>();
        _entityManager = IoCManager.Resolve<IEntityManager>();

        _halfExtent = ComputeHalfExtent(config);
    }

    private Vector2 ComputeHalfExtent(DungeonConfigPrototype config)
    {
        if (config.Generator is not PrefabDunGen prefab || prefab.RoomWhitelist.Count == 0)
            return new Vector2(8f, 8f); // sensible fallback

        var maxW = 0;
        var maxH = 0;
        foreach (var room in _prototypes.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            foreach (var tag in room.Tags)
            {
                if (!prefab.RoomWhitelist.Contains(tag))
                    continue;
                if (room.Size.X > maxW) maxW = room.Size.X;
                if (room.Size.Y > maxH) maxH = room.Size.Y;
                break;
            }
        }
        // Pad by a generous factor: the dungeon will assemble multiple rooms plus corridors.
        return new Vector2(maxW * 2f + 4f, maxH * 2f + 4f);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_manager.CurrentMode == null)
            return;

        var mouseCoords = _manager.CurrentMode.MouseCoords;
        if (!mouseCoords.IsValid(_entityManager))
            return;

        var xformSystem = _entityManager.System<SharedTransformSystem>();
        var worldPos = xformSystem.ToMapCoordinates(mouseCoords).Position;

        // Snap to tile grid
        var snappedX = MathF.Floor(worldPos.X);
        var snappedY = MathF.Floor(worldPos.Y);
        var origin = new Vector2(snappedX, snappedY);

        var box = new Box2(origin - _halfExtent, origin + _halfExtent);

        args.WorldHandle.DrawRect(box, FillColor);
        args.WorldHandle.DrawRect(box, BorderColor, filled: false);

        // Draw a small crosshair at the exact spawn origin
        const float cross = 0.4f;
        args.WorldHandle.DrawLine(origin + new Vector2(-cross, 0), origin + new Vector2(cross, 0), OriginColor);
        args.WorldHandle.DrawLine(origin + new Vector2(0, -cross), origin + new Vector2(0, cross), OriginColor);
    }
}
