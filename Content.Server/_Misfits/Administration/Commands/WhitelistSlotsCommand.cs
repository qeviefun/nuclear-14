// #Misfits Change - Console command to open the whitelist job slots admin panel
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared._Misfits.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Administration.Commands;

/// <summary>
/// Opens the whitelist job slots panel so admins can search for players and manage station job slots.
/// </summary>
[AdminCommand(AdminFlags.Whitelist)]
public sealed class WhitelistSlotsCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "whitelistslots";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new WhitelistSearchEui(WhitelistSearchMode.JobSlots);
        _eui.OpenEui(ui, player);
    }
}
