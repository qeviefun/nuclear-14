// #Misfits Add - UI Controller for the Persistent Decal Spawn Window.
// Opens/closes a PersistentDecalSpawnWindow and populates decal prototypes.
// Used in the admin Server tab alongside the Entity/Tile persistent spawn buttons.
using Content.Shared.Decals;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Misfits.PersistentSpawn;

/// <summary>
/// Manages the Persistent Decal Spawn window lifecycle and prototype refresh.
/// </summary>
public sealed class PersistentDecalSpawnUIController : UIController
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private PersistentDecalSpawnWindow? _window;

    // ── Public API ─────────────────────────────────────────────────────────────

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
            _window.Close();
        else
            _window.Open();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Close();
    }

    // ── Window management ──────────────────────────────────────────────────────

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<PersistentDecalSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);

        // Populate decal list on first open
        ReloadPrototypes();
    }

    private void ReloadPrototypes()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Populate(_prototypes.EnumeratePrototypes<DecalPrototype>());
    }
}
