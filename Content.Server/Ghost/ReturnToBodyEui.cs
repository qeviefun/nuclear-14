using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Content.Shared.Mind;

namespace Content.Server.Ghost;

public sealed class ReturnToBodyEui : BaseEui
{
    private readonly SharedMindSystem _mindSystem;

    private readonly MindComponent _mind;

    // #Misfits Add - Optional callback invoked when the player accepts. Used by the
    // resuscitation flow so the body is only revived after the ghost consents — denying
    // the prompt now leaves the corpse dead instead of silently reviving it.
    private readonly Action? _onAccept;

    public ReturnToBodyEui(MindComponent mind, SharedMindSystem mindSystem)
        : this(mind, mindSystem, null)
    {
    }

    // #Misfits Add - Overload that runs an action on Accept (e.g. perform the actual revive).
    public ReturnToBodyEui(MindComponent mind, SharedMindSystem mindSystem, Action? onAccept)
    {
        _mind = mind;
        _mindSystem = mindSystem;
        _onAccept = onAccept;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ReturnToBodyMessage choice ||
            !choice.Accepted)
        {
            Close();
            return;
        }

        // #Misfits Change - Run the consent-gated revive (if any) before transferring
        // the mind back, so the body is alive the moment the player re-enters it.
        _onAccept?.Invoke();

        _mindSystem.UnVisit(_mind.Session);

        Close();
    }
}
