// #Misfits Change - Client-side system that receives the admin list event and opens the popup
using Content.Client._Misfits.Administration.UI;
using Content.Shared._Misfits.Administration;

namespace Content.Client._Misfits.Administration.Systems;

public sealed class AdminListSystem : EntitySystem
{
    private AdminListWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AdminListEvent>(OnAdminListReceived);
    }

    private void OnAdminListReceived(AdminListEvent ev)
    {
        // Close any existing window before opening a new one
        if (_window is { Disposed: false })
        {
            _window.Close();
        }

        _window = new AdminListWindow();
        _window.Populate(ev);
        _window.OpenCentered();
    }
}
