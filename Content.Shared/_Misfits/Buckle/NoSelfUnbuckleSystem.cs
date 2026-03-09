// #Misfits Change /Add:/ Prevent self-unbuckling from selected strap entities.
using Content.Shared.Buckle.Components;
using Content.Shared.Popups;

namespace Content.Shared._Misfits.Buckle;

public sealed class NoSelfUnbuckleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoSelfUnbuckleComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
    }

    private void OnUnstrapAttempt(Entity<NoSelfUnbuckleComponent> ent, ref UnstrapAttemptEvent args)
    {
        if (args.User == null || args.User != args.Buckle.Owner)
            return;

        args.Cancelled = true;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString(ent.Comp.Popup), ent, args.User.Value);
    }
}