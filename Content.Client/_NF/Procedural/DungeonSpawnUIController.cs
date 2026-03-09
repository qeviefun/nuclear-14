using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.IoC;

namespace Content.Client._NF.Procedural;

/// <summary>
/// UIController that manages the lifecycle of the DungeonSpawnWindow.
/// Can be retrieved via <c>UIManager.GetUIController&lt;DungeonSpawnUIController&gt;()</c>
/// from both the Sandbox panel and the Admin menu.
/// </summary>
[UsedImplicitly]
public sealed class DungeonSpawnUIController : UIController
{
    private DungeonSpawnWindow? _window;

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
            _window.Close();
        else
            _window.Open();
    }

    public void OpenWindow()
    {
        EnsureWindow();
        if (!_window!.IsOpen)
            _window.Open();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<DungeonSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
    }
}
