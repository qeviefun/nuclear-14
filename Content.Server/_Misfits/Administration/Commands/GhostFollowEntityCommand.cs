// #Misfits Add - Admin console command: enter aghost mode (if not already) then follow a target entity by NetEntity ID.
// Used by the Admin Menu Objects tab when clicking an entry to ghost-follow it.
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._Misfits.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class GhostFollowEntityCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IServerConsoleHost _consoleHost = default!;

    public string Command => "ghostfollow";
    public string Description => "Enters aghost mode (if needed) and starts following the given entity.";
    public string Help => "ghostfollow <net entity id>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError("Only players can use this command.");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEnt) || !_entManager.TryGetEntity(netEnt, out var target))
        {
            shell.WriteError($"Could not find entity with ID '{args[0]}'.");
            return;
        }

        // If the admin is not already an admin ghost (CanGhostInteract), enter aghost first.
        // This runs synchronously server-side so player.AttachedEntity is updated before we call follow.
        var alreadyAGhost = player.AttachedEntity.HasValue
            && _entManager.TryGetComponent<GhostComponent>(player.AttachedEntity.Value, out var ghost)
            && ghost.CanGhostInteract;

        if (!alreadyAGhost)
            _consoleHost.ExecuteCommand(player, "aghost");

        // Start following the target entity with the admin's (now ghost) entity.
        if (player.AttachedEntity != null && _entManager.TrySystem<FollowerSystem>(out var follower))
            follower.StartFollowingEntity(player.AttachedEntity.Value, target.Value);
    }
}
