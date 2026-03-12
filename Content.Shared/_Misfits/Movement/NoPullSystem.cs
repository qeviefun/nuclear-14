// #Misfits Change /Add/ - System that prevents entities with NoPullComponent from being dragged.
using Content.Shared._Misfits.Movement;
using Content.Shared.Pulling.Events; // #Misfits Fix: BeingPulledAttemptEvent lives in Pulling.Events, not Movement.Pulling.Events

namespace Content.Shared._Misfits.Movement;

/// <summary>
/// Cancels any attempt to pull/drag an entity that has <see cref="NoPullComponent"/>.
/// This covers both player drag-interactions and NPC pull actions.
/// </summary>
public sealed class NoPullSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NoPullComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
    }

    private void OnBeingPulledAttempt(EntityUid uid, NoPullComponent component, BeingPulledAttemptEvent args)
    {
        // This entity is too heavy / anchored by its own drive system to be dragged.
        args.Cancel();
    }
}
