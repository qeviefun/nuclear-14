// #Misfits Add - Admin-accessible wrapper for the existing DecalPlacerWindow.
// The vanilla DecalPlacerUIController gates window opening behind the sandbox flag.
// This controller bypasses that check so server admins with the Spawn flag can
// use the decal placer from the admin Server tab without enabling sandbox.
using Content.Client.Decals.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Decals;

namespace Content.Client._Misfits.PersistentSpawn;

/// <summary>
/// Opens the vanilla <see cref="DecalPlacerWindow"/> without requiring sandbox mode.
/// Decal placement on the server side already checks <c>AdminFlags.Spawn</c>, so
/// non-admin clients simply won't be able to place decals regardless.
/// </summary>
public sealed class AdminDecalPlacerUIController : UIController
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private DecalPlacerWindow? _window;

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

        _window = UIManager.CreateWindow<DecalPlacerWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);

        // Populate decal list - reuse existing Populate() method
        _window.Populate(_prototypes.EnumeratePrototypes<DecalPrototype>());
    }
}
