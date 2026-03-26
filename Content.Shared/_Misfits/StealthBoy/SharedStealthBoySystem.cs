// #Misfits Add - Shared Stealth Boy logic. Handles activation, opacity interpolation,
// and expiry for the Fallout Stealth Boy device. Ported/inspired by RMC-14 stealth system,
// simplified to remove evasion and skill dependencies.
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Misfits.StealthBoy;

public abstract class SharedStealthBoySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StealthBoyComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<StealthBoyComponent, ActivateStealthBoyActionEvent>(OnActivateAction);
        SubscribeLocalEvent<StealthBoyActiveComponent, ComponentShutdown>(OnActiveShutdown);
    }

    private void OnUseInHand(Entity<StealthBoyComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<StealthBoyActiveComponent>(args.User) || HasComp<StealthComponent>(args.User))
        {
            if (_net.IsServer)
                _popup.PopupEntity("You are already cloaked.", ent, args.User);
            return;
        }

        args.Handled = true;
        Activate(ent, args.User);
    }

    private void OnActivateAction(Entity<StealthBoyComponent> ent, ref ActivateStealthBoyActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<StealthBoyActiveComponent>(args.Performer) || HasComp<StealthComponent>(args.Performer))
        {
            if (_net.IsServer)
                _popup.PopupEntity("You are already cloaked.", ent, args.Performer);
            args.Handled = true;
            return;
        }

        args.Handled = true;
        Activate(ent, args.Performer);
    }

    protected void Activate(Entity<StealthBoyComponent> item, EntityUid user)
    {
        var now = _timing.CurTime;
        var active = EnsureComp<StealthBoyActiveComponent>(user);
        active.StartTime = now;
        active.EndTime = now + item.Comp.Duration;
        active.TargetVisibility = item.Comp.Visibility;
        active.FadeInTime = item.Comp.FadeInTime;
        active.FadeOutTime = item.Comp.FadeOutTime;
        active.FadingOut = false;
        active.FadeOutStart = TimeSpan.Zero;
        Dirty(user, active);

        EnsureComp<StealthComponent>(user);
        _stealth.SetEnabled(user, true);
        _stealth.SetVisibility(user, 1f);

        if (_net.IsServer)
            _popup.PopupEntity("The Stealth Boy hums and you feel yourself fade from view.", user, user);
    }

    private void OnActiveShutdown(Entity<StealthBoyActiveComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent))
            return;

        RemCompDeferred<StealthComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<StealthBoyActiveComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            var now = _timing.CurTime;

            if (!active.FadingOut)
            {
                // Fade in phase: interpolate visibility down to the stealth threshold.
                var fadeElapsed = (now - active.StartTime) / active.FadeInTime;
                var visibility = active.FadeInTime > TimeSpan.Zero
                    ? (float)(1.0 + (active.TargetVisibility - 1.0) * Math.Min(1.0, fadeElapsed))
                    : active.TargetVisibility;

                SetVisibility(uid, visibility);

                if (now >= active.EndTime)
                {
                    active.FadingOut = true;
                    active.FadeOutStart = now;
                    Dirty(uid, active);
                    continue;
                }
            }
            else
            {
                // Fade out phase: interpolate visibility back to normal.
                var fadeOutElapsed = (now - active.FadeOutStart) / active.FadeOutTime;
                var visibility = active.FadeOutTime > TimeSpan.Zero
                    ? (float)(active.TargetVisibility + (1.0 - active.TargetVisibility) * Math.Min(1.0, fadeOutElapsed))
                    : 1f;

                SetVisibility(uid, visibility);

                if (fadeOutElapsed >= 1.0)
                {
                    _stealth.SetVisibility(uid, 1f);
                    RemCompDeferred<StealthComponent>(uid);
                    RemCompDeferred<StealthBoyActiveComponent>(uid);
                    if (_net.IsServer)
                        _popup.PopupEntity("You reappear as the Stealth Boy power fades.", uid, uid);
                    continue;
                }
            }
        }
    }

    private void SetVisibility(EntityUid uid, float visibility)
    {
        if (!HasComp<StealthComponent>(uid))
            return;

        _stealth.SetVisibility(uid, visibility);
    }
}

/// <summary>
/// Fired when the Stealth Boy hotkey button is pressed so the item can activate from hand or worn slots.
/// </summary>
public sealed partial class ActivateStealthBoyActionEvent : InstantActionEvent;
