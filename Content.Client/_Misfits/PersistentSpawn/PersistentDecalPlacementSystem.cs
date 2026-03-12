// #Misfits Add - Client-side persistent decal placement system.
// Mirrors DecalPlacementSystem (Content.Client/Decals/DecalPlacementSystem.cs) but raises
// PersistentDecalSpawnRequestEvent / PersistentDecalEraseRequestEvent so the server
// stores the decal to JSON and re-applies it every round.
using System.Numerics;
using Content.Shared._Misfits.PersistentSpawn;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client._Misfits.PersistentSpawn;

/// <summary>
/// Handles mouse-based placement and erasure of persistent decals on the client.
/// Sends <see cref="PersistentDecalSpawnRequestEvent"/> (left-click) and
/// <see cref="PersistentDecalEraseRequestEvent"/> (right-click) to the server.
/// </summary>
public sealed class PersistentDecalPlacementSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    // ── Current decal settings ─────────────────────────────────────────────────

    private string? _decalId;
    private Color _decalColor = Color.White;
    private Angle _decalAngle = Angle.Zero;
    private bool _snap;
    private int _zIndex;
    private bool _cleanable;

    // ── State flags ────────────────────────────────────────────────────────────

    private bool _active;
    private bool _placing;
    private bool _erasing;

    // ── Public accessors ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the active decal data for the overlay cursor.
    /// Signature matches DecalPlacementSystem.GetActiveDecal() so the same overlay pattern applies.
    /// </summary>
    public (DecalPrototype? Decal, bool Snap, Angle Angle, Color Color) GetActiveDecal()
    {
        return _active && _decalId != null
            ? (_protoMan.Index<DecalPrototype>(_decalId), _snap, _decalAngle, _decalColor)
            : (null, false, Angle.Zero, Color.Wheat);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize();

        // Adds its own cursor overlay (separate from DecalPlacementOverlay)
        _overlayManager.AddOverlay(new PersistentDecalPlacementOverlay(this, _transform, _sprite));

        // Bind the same editor keys as vanilla DecalPlacementSystem.
        // Each system registers with its own type so they won't interfere.
        // Only the one with _active=true actually processes the event.
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(
                (session, coords, uid) =>
                {
                    if (!_active || _placing || _decalId == null)
                        return false;

                    _placing = true;

                    if (_snap)
                    {
                        coords = coords.WithPosition(new Vector2(
                            (float)(MathF.Round(coords.X - 0.5f, MidpointRounding.AwayFromZero) + 0.5),
                            (float)(MathF.Round(coords.Y - 0.5f, MidpointRounding.AwayFromZero) + 0.5)));
                    }

                    coords = coords.Offset(new Vector2(-0.5f, -0.5f));

                    if (!coords.IsValid(EntityManager))
                        return false;

                    var mapCoords = _transform.ToMapCoordinates(coords);

                    RaiseNetworkEvent(new PersistentDecalSpawnRequestEvent(
                        _decalId,
                        mapCoords.Position.X,
                        mapCoords.Position.Y,
                        (float)_decalAngle.Degrees,
                        _decalColor.ToArgb(),
                        _snap,
                        _zIndex,
                        _cleanable));

                    return true;
                },
                (session, coords, uid) =>
                {
                    if (!_active)
                        return false;

                    _placing = false;
                    return true;
                }, true))
            .Bind(EngineKeyFunctions.EditorCancelPlace, new PointerStateInputCmdHandler(
                (session, coords, uid) =>
                {
                    if (!_active || _erasing)
                        return false;

                    _erasing = true;

                    var mapCoords = _transform.ToMapCoordinates(coords);

                    RaiseNetworkEvent(new PersistentDecalEraseRequestEvent(
                        mapCoords.Position.X,
                        mapCoords.Position.Y));

                    return true;
                },
                (session, coords, uid) =>
                {
                    if (!_active)
                        return false;

                    _erasing = false;
                    return true;
                }, true))
            .Register<PersistentDecalPlacementSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay<PersistentDecalPlacementOverlay>();
        CommandBinds.Unregister<PersistentDecalPlacementSystem>();
    }

    // ── Public API used by PersistentDecalSpawnUIController ───────────────────

    /// <summary>Update the current decal selection and appearance parameters.</summary>
    public void UpdateDecalInfo(string id, Color color, float rotation, bool snap, int zIndex, bool cleanable)
    {
        _decalId = id;
        _decalColor = color;
        _decalAngle = Angle.FromDegrees(rotation);
        _snap = snap;
        _zIndex = zIndex;
        _cleanable = cleanable;
    }

    /// <summary>
    /// Enable or disable placement mode. When enabled, switches to the editor input context
    /// so mouse clicks reach the command binds above.
    /// </summary>
    public void SetActive(bool active)
    {
        _active = active;
        if (_active)
            _inputManager.Contexts.SetActiveContext("editor");
        else
            _inputSystem.SetEntityContextActive();
    }
}
