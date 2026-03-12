// #Misfits Change /Add/ - Escalates a repeated grab into a choking carry with a fixed 70% movement debuff.
using Content.Server.Chat.Systems;
using Content.Server._Misfits.Grabbing.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Carrying;
using Content.Shared.Chat;
using Content.Shared._Misfits.Movement.Pulling.Events;
using Content.Shared.Carrying;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.Grabbing;

public sealed class DoubleGrabSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly CarryingSystem _carrying = default!;
    [Dependency] private readonly CarryingSlowdownSystem _carryingSlowdown = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RepeatPullAttemptEvent>(OnRepeatPullAttempt);
        SubscribeLocalEvent<DoubleGrabPendingVictimComponent, MoveInputEvent>(OnPendingVictimMoveInput);
        SubscribeLocalEvent<DoubleGrabPendingCarrierComponent, ComponentShutdown>(OnPendingCarrierShutdown);
        SubscribeLocalEvent<DoubleGrabPendingVictimComponent, ComponentShutdown>(OnPendingVictimShutdown);
        SubscribeLocalEvent<DoubleGrabCarrierComponent, ComponentShutdown>(OnCarrierShutdown);
        SubscribeLocalEvent<DoubleGrabVictimComponent, ComponentShutdown>(OnVictimShutdown);
    }

    private void OnRepeatPullAttempt(ref RepeatPullAttemptEvent args)
    {
        if (TryComp<DoubleGrabPendingCarrierComponent>(args.User, out var pending) && pending.Victim == args.Target)
        {
            args.Handled = true;
            return;
        }

        if (HasComp<DoubleGrabCarrierComponent>(args.User) ||
            HasComp<BeingCarriedComponent>(args.User) ||
            !HasComp<CarriableComponent>(args.Target))
        {
            return;
        }

        StartPendingDoubleGrab(args.User, args.Target);
        args.Handled = true;
    }

    private void OnPendingVictimMoveInput(Entity<DoubleGrabPendingVictimComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        _chat.TrySendInGameICMessage(ent.Owner,
            Loc.GetString("misfits-chat-double-grab-resist", ("carrier", Identity.Entity(ent.Comp.Carrier, EntityManager))),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);

        StopPendingDoubleGrab(ent.Comp.Carrier, ent.Owner);
    }

    private void OnPendingCarrierShutdown(Entity<DoubleGrabPendingCarrierComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<DoubleGrabPendingVictimComponent>(ent.Comp.Victim, out var victimComp) &&
            victimComp.Carrier == ent.Owner)
        {
            RemComp<DoubleGrabPendingVictimComponent>(ent.Comp.Victim);
        }
    }

    private void OnPendingVictimShutdown(Entity<DoubleGrabPendingVictimComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<DoubleGrabPendingCarrierComponent>(ent.Comp.Carrier, out var carrierComp) &&
            carrierComp.Victim == ent.Owner)
        {
            RemComp<DoubleGrabPendingCarrierComponent>(ent.Comp.Carrier);
        }
    }

    private void OnCarrierShutdown(Entity<DoubleGrabCarrierComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<DoubleGrabVictimComponent>(ent.Comp.Victim, out var victimComp) &&
            victimComp.Carrier == ent.Owner)
        {
            RemComp<DoubleGrabVictimComponent>(ent.Comp.Victim);
        }
    }

    private void OnVictimShutdown(Entity<DoubleGrabVictimComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<DoubleGrabCarrierComponent>(ent.Comp.Carrier, out var carrierComp) &&
            carrierComp.Victim == ent.Owner)
        {
            RemComp<DoubleGrabCarrierComponent>(ent.Comp.Carrier);

            if (TryComp<CarryingComponent>(ent.Comp.Carrier, out var carrying) && carrying.Carried == ent.Owner)
                _carrying.RecalculateCarrySlowdown(ent.Comp.Carrier, ent.Owner);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DoubleGrabCarrierComponent>();
        while (query.MoveNext(out var carrier, out var choke))
        {
            if (!TryComp<CarryingComponent>(carrier, out var carrying) ||
                carrying.Carried != choke.Victim ||
                !TryComp<DoubleGrabVictimComponent>(choke.Victim, out var victimComp) ||
                !TryComp<BeingCarriedComponent>(choke.Victim, out var beingCarried) ||
                beingCarried.Carrier != carrier)
            {
                StopDoubleGrab(carrier, choke, restoreCarrySlowdown: false);
                continue;
            }

            if (_mobState.IsDead(choke.Victim) || _mobState.IsDead(carrier))
            {
                StopDoubleGrab(carrier, choke);
                continue;
            }

            choke.HeldTime += TimeSpan.FromSeconds(frameTime);

            if (choke.HeldTime >= choke.SuffocationStartTime &&
                TryComp<RespiratorComponent>(choke.Victim, out var respirator))
            {
                _respirator.UpdateSaturation(choke.Victim, -frameTime * choke.SuffocationDrainPerSecond, respirator);

                if (respirator.Saturation < respirator.SuffocationThreshold &&
                    _gameTiming.CurTime >= victimComp.NextGaspEmoteTime)
                {
                    victimComp.NextGaspEmoteTime = _gameTiming.CurTime + victimComp.GaspEmoteCooldown;
                    _chat.TrySendInGameICMessage(choke.Victim,
                        Loc.GetString("misfits-chat-double-grab-gasp"),
                        InGameICChatType.Emote,
                        ChatTransmitRange.Normal,
                        ignoreActionBlocker: true);
                }
            }

            if (choke.CritApplied || choke.HeldTime < choke.CritTime)
                continue;

            ForceVictimCritical(carrier, choke.Victim);
            choke.CritApplied = true;
        }

        var pendingQuery = EntityQueryEnumerator<DoubleGrabPendingCarrierComponent>();
        while (pendingQuery.MoveNext(out var carrier, out var pending))
        {
            if (!TryComp<DoubleGrabPendingVictimComponent>(pending.Victim, out var pendingVictim) ||
                pendingVictim.Carrier != carrier ||
                !TryComp<PullerComponent>(carrier, out var puller) ||
                puller.Pulling != pending.Victim ||
                _mobState.IsDead(carrier) ||
                _mobState.IsDead(pending.Victim))
            {
                StopPendingDoubleGrab(carrier, pending.Victim);
                continue;
            }

            pending.HeldTime += TimeSpan.FromSeconds(frameTime);
            if (pending.HeldTime < pending.PinTime)
                continue;

            StopPendingDoubleGrab(carrier, pending.Victim);
            if (!_carrying.TryCarry(carrier, pending.Victim))
                continue;

            StartDoubleGrab(carrier, pending.Victim);
        }
    }

    private void StartPendingDoubleGrab(EntityUid carrier, EntityUid victim)
    {
        var carrierComp = EnsureComp<DoubleGrabPendingCarrierComponent>(carrier);
        carrierComp.Victim = victim;
        carrierComp.HeldTime = TimeSpan.Zero;

        var victimComp = EnsureComp<DoubleGrabPendingVictimComponent>(victim);
        victimComp.Carrier = carrier;
    }

    private void StopPendingDoubleGrab(EntityUid carrier, EntityUid victim)
    {
        if (TryComp<DoubleGrabPendingVictimComponent>(victim, out var victimComp) && victimComp.Carrier == carrier)
            RemComp<DoubleGrabPendingVictimComponent>(victim);

        if (TryComp<DoubleGrabPendingCarrierComponent>(carrier, out var carrierComp) && carrierComp.Victim == victim)
            RemComp<DoubleGrabPendingCarrierComponent>(carrier);
    }

    private void StartDoubleGrab(EntityUid carrier, EntityUid victim)
    {
        var carrierComp = EnsureComp<DoubleGrabCarrierComponent>(carrier);
        carrierComp.Victim = victim;
        carrierComp.HeldTime = TimeSpan.Zero;
        carrierComp.CritApplied = false;

        var victimComp = EnsureComp<DoubleGrabVictimComponent>(victim);
        victimComp.Carrier = carrier;
        victimComp.NextGaspEmoteTime = _gameTiming.CurTime;

        _chat.TrySendInGameICMessage(carrier,
            Loc.GetString("misfits-chat-double-grab-cinch", ("victim", Identity.Entity(victim, EntityManager))),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);

        _chat.TrySendInGameICMessage(victim,
            Loc.GetString("misfits-chat-double-grab-victim", ("carrier", Identity.Entity(carrier, EntityManager))),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);

        if (TryComp<CarryingSlowdownComponent>(carrier, out var slowdown))
            _carryingSlowdown.SetModifier(carrier, carrierComp.CarrySpeedModifier, carrierComp.CarrySpeedModifier, slowdown);
    }

    private void StopDoubleGrab(EntityUid carrier, DoubleGrabCarrierComponent choke, bool restoreCarrySlowdown = true)
    {
        var victim = choke.Victim;

        if (TryComp<DoubleGrabVictimComponent>(victim, out var victimComp) && victimComp.Carrier == carrier)
            RemComp<DoubleGrabVictimComponent>(victim);

        RemComp<DoubleGrabCarrierComponent>(carrier);

        if (restoreCarrySlowdown &&
            TryComp<CarryingComponent>(carrier, out var carrying) &&
            carrying.Carried == victim)
        {
            _carrying.RecalculateCarrySlowdown(carrier, victim);
        }
    }

    private void ForceVictimCritical(EntityUid carrier, EntityUid victim)
    {
        if (!_mobThreshold.TryGetThresholdForState(victim, MobState.Critical, out var critThreshold) ||
            !TryComp<DamageableComponent>(victim, out var damageable) ||
            !TryComp<MobStateComponent>(victim, out var mobState))
        {
            return;
        }

        if (mobState.CurrentState == MobState.Critical || mobState.CurrentState == MobState.Dead)
            return;

        var remainingDamage = critThreshold.Value - damageable.TotalDamage;
        if (remainingDamage <= FixedPoint2.Zero)
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict["Asphyxiation"] = remainingDamage + FixedPoint2.New(1);
        _damageable.TryChangeDamage(victim, damage, origin: carrier, partMultiplier: 0f);
    }
}