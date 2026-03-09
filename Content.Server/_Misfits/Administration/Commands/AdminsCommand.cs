// #Misfits Change - /admins player command to show online staff
using Content.Server._Misfits.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Administration.Commands;

/// <summary>
/// Player-accessible command that opens a popup showing online admins and mentors.
/// No admin flag required — any connected player can run this.
/// </summary>
[AnyCommand]
public sealed class AdminsCommand : IConsoleCommand
{
    public string Command => "admins";
    public string Description => "Shows a list of online admins and mentors.";
    public string Help => "Usage: admins";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError("This command can only be run by a player.");
            return;
        }

        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AdminListSystem>();
        system.SendAdminList(shell.Player);
    }
}
