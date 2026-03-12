// #Misfits Change /Add/ Offline map editor open command.
// Kept in Content.Client so the launcher can use the normal client binary without side-loading
// a mixed client/server assembly that fails sandbox verification.
using Content.Shared.Administration;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Client.MapEditor;

[AnyCommand]
public sealed class MapEditorOpenCommand : IConsoleCommand
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const int EditorMapId = 4242;
    private string? _pendingMapPath;

    public string Command => "mapeditor-open";
    public string Description => "Open a map in the dedicated map editor session.";
    public string Help => $"Usage: {Command} <resource or mounted map path>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        _pendingMapPath = args[0].Replace('\\', '/');
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _playerManager.LocalPlayerAttached -= OnLocalPlayerAttached;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _playerManager.LocalPlayerAttached += OnLocalPlayerAttached;

        TryOpenPendingMap();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.Session != _playerManager.LocalSession)
            return;

        if (args.NewStatus != SessionStatus.InGame)
            return;

        TryOpenPendingMap();
    }

    private void OnLocalPlayerAttached(EntityUid _)
    {
        TryOpenPendingMap();
    }

    private void TryOpenPendingMap()
    {
        if (string.IsNullOrWhiteSpace(_pendingMapPath))
            return;

        var session = _playerManager.LocalSession;
        if (session == null || session.Status != SessionStatus.InGame || session.AttachedEntity == null)
            return;

        _consoleHost.RemoteExecuteCommand(null, $"mapping {EditorMapId} {QuotePath(_pendingMapPath)}");
        _pendingMapPath = null;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _playerManager.LocalPlayerAttached -= OnLocalPlayerAttached;
    }

    private static string QuotePath(string path)
    {
        return path.Contains(' ')
            ? $"\"{path.Replace("\"", "\\\"")}\""
            : path;
    }
}