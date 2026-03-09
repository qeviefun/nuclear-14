// #Misfits Change - /players player command opens the staff window with connected player count
using Content.Server._Misfits.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Administration.Commands;

/// <summary>
/// Player-accessible alias for the staff window shown by /admins.
/// </summary>
[AnyCommand]
public sealed class PlayersCommand : IConsoleCommand
{
    public string Command => "players";
    public string Description => "Shows online staff and the connected player count.";
    public string Help => "Usage: players";

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