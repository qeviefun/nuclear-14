// #Misfits Change /Add/ Offline map editor server bootstrap.
// Promotes the local host into mapping permissions when the dedicated launcher enables the
// mapeditor cvar, so the editor can immediately load into mapping mode after connect.
using Content.Server.Administration.Managers;
using Content.Shared._Misfits.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.MapEditor;

public sealed class MapEditorServerBootstrapSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private bool _enabled;
    private bool _promoted;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, MapEditorCVars.Enabled, enabled => _enabled = enabled, true);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_enabled || _promoted)
            return;

        _adminManager.PromoteHost(ev.Player);
        _promoted = true;
    }
}