using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Supporter;

[AdminCommand(AdminFlags.Admin)]
public sealed class SupporterManagerCommand : IConsoleCommand
{
    [Dependency] private readonly EuiManager _eui = default!;

    public string Command => "opensupportermanager";
    public string Description => "Opens the Supporter Manager GUI.";
    public string Help => "opensupportermanager";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError("This command must be run by a player.");
            return;
        }

        var eui = new SupporterManagerEui();
        _eui.OpenEui(eui, admin);
    }
}
