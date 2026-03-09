// #Misfits Change - Console command to open the Whitelist Search admin panel
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared._Misfits.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Administration.Commands;

/// <summary>
/// Opens the role whitelist panel so admins can search for players and manage their job whitelists.
/// </summary>
// #Misfits Change - Whitelist flag not universally assigned; gate on Admin instead.
[AdminCommand(AdminFlags.Admin)]
public sealed class WhitelistSearchCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "whitelistsearch";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new WhitelistSearchEui();
        _eui.OpenEui(ui, player);
    }
}
