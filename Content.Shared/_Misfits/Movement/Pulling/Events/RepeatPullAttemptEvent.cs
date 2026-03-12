// #Misfits Change /Add/ - Raised when a user tries to pull the same target again instead of toggling the pull off.
namespace Content.Shared._Misfits.Movement.Pulling.Events;

[ByRefEvent]
public struct RepeatPullAttemptEvent
{
    public RepeatPullAttemptEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
        Handled = false;
    }

    public EntityUid User;
    public EntityUid Target;
    public bool Handled;
}