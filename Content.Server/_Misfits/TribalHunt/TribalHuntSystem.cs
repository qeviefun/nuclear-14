using Content.Server.Actions;
using Content.Server.Nutrition.Components;
using Content.Shared._Misfits.TribalHunt;
using Content.Shared._Misfits.Warcry;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Map;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.TribalHunt;

/// <summary>
/// Cooperative tribal PvE loop: elders start timed hunts and tribe members offer food trophies to complete them.
/// Completion grants a temporary tribe-wide speed blessing.
/// </summary>
public sealed class TribalHuntSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LegendaryCreatureSpawnerSystem _legendarySpawner = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private bool _activeHunt;
    private TimeSpan _huntExpiresAt;
    private int _requiredOfferings;
    private int _offeredSoFar;
    private TimeSpan _rewardDuration;
    private float _rewardSpeedBonus;
    private EntityUid? _activeLegendaryCreature;
    private EntityUid? _activeHuntSessionId;
    private TimeSpan _lastLocationBroadcast;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TribalHuntLeaderComponent, ComponentStartup>(OnLeaderStartup);
        SubscribeLocalEvent<TribalHuntLeaderComponent, ComponentShutdown>(OnLeaderShutdown);
        SubscribeLocalEvent<TribalHuntLeaderComponent, PerformTribalStartHuntActionEvent>(OnStartHuntAction);

        SubscribeLocalEvent<TribalHuntParticipantComponent, ComponentStartup>(OnParticipantStartup);
        SubscribeLocalEvent<TribalHuntParticipantComponent, ComponentShutdown>(OnParticipantShutdown);
        SubscribeLocalEvent<TribalHuntParticipantComponent, PerformTribalOfferTrophyActionEvent>(OnOfferAction);

        SubscribeLocalEvent<LegendaryCreatureComponent, ComponentShutdown>(OnLegendaryCreatureShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_activeHunt)
            return;

        // Check if legendary creature has been killed
        if (_activeLegendaryCreature != null && !Exists(_activeLegendaryCreature))
        {
            _activeLegendaryCreature = null;
            // Creature death counts as hunt completion
            var leader = EntityQuery<TribalHuntLeaderComponent>();
            foreach (var leaderComp in leader)
            {
                CompleteHunt(leaderComp.TargetDepartment);
                break;  // Only one hunt at a time
            }
            return;
        }

        // Periodically broadcast legendary creature location (every 15 seconds)
        if (_activeLegendaryCreature != null && _timing.CurTime >= _lastLocationBroadcast + TimeSpan.FromSeconds(15))
        {
            _lastLocationBroadcast = _timing.CurTime;
            BroadcastCreatureLocation();
        }

        if (_timing.CurTime < _huntExpiresAt)
            return;

        _activeHunt = false;
        _activeLegendaryCreature = null;
        BroadcastToDepartment("Tribe", "tribal-hunt-popup-legendary-escaped");
    }

    private void BroadcastCreatureLocation()
    {
        if (_activeLegendaryCreature == null || !Exists(_activeLegendaryCreature))
            return;

        if (!TryComp<TransformComponent>(_activeLegendaryCreature, out var creatureXform))
            return;

        var location = creatureXform.Coordinates;
        BroadcastToDepartment("Tribe",
            "tribal-hunt-popup-creature-location",
            ("location", location));
    }

    private void OnLeaderStartup(EntityUid uid, TribalHuntLeaderComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.StartActionEntity, component.StartAction);
    }

    private void OnLeaderShutdown(EntityUid uid, TribalHuntLeaderComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.StartActionEntity);
    }

    private void OnParticipantStartup(EntityUid uid, TribalHuntParticipantComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.OfferActionEntity, component.OfferAction);
    }

    private void OnParticipantShutdown(EntityUid uid, TribalHuntParticipantComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.OfferActionEntity);
    }

    private void OnStartHuntAction(EntityUid uid, TribalHuntLeaderComponent component, PerformTribalStartHuntActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!CanLeadHunt(uid, component))
        {
            _popup.PopupEntity(Loc.GetString("tribal-hunt-popup-cannot-lead"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (_activeHunt)
        {
            var remaining = (_huntExpiresAt - _timing.CurTime).TotalSeconds;
            if (remaining < 0)
                remaining = 0;

            _popup.PopupEntity(
                Loc.GetString("tribal-hunt-popup-already-active", ("remaining", MathF.Ceiling((float) remaining))),
                uid,
                uid,
                PopupType.SmallCaution);
            return;
        }

        _activeHunt = true;
        _offeredSoFar = 0;
        _requiredOfferings = Math.Max(1, component.RequiredOfferings);
        _huntExpiresAt = _timing.CurTime + component.HuntDuration;
        _rewardDuration = component.RewardDuration;
        _rewardSpeedBonus = component.RewardSpeedBonus;
        _activeHuntSessionId = uid;  // Use leader as session ID
        _lastLocationBroadcast = _timing.CurTime;  // Initialize broadcast timer

        // Spawn legendary creature on the map
        if (TryComp<TransformComponent>(uid, out var leaderXform) && leaderXform.MapID != MapId.Nullspace)
        {
            _activeLegendaryCreature = _legendarySpawner.TrySpawnLegendaryCreature(
                "TribalLegendaryBeast",
                uid,
                leaderXform.MapID);

            if (_activeLegendaryCreature != null)
            {
                BroadcastToDepartment(component.TargetDepartment,
                    "tribal-hunt-popup-legendary-started",
                    ("leader", uid),
                    ("creature", _activeLegendaryCreature));
            }
        }

        BroadcastToDepartment(component.TargetDepartment,
            "tribal-hunt-popup-started",
            ("leader", uid),
            ("required", _requiredOfferings),
            ("minutes", MathF.Ceiling((float) component.HuntDuration.TotalMinutes)));
    }

    private void OnOfferAction(EntityUid uid, TribalHuntParticipantComponent component, PerformTribalOfferTrophyActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!IsInDepartment(uid, component.TargetDepartment))
        {
            _popup.PopupEntity(Loc.GetString("tribal-hunt-popup-not-tribe"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (!_activeHunt)
        {
            _popup.PopupEntity(Loc.GetString("tribal-hunt-popup-no-active"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (!TryGetHeldFood(uid, out var offering))
        {
            _popup.PopupEntity(Loc.GetString("tribal-hunt-popup-needs-trophy"), uid, uid, PopupType.SmallCaution);
            return;
        }

        QueueDel(offering);

        _offeredSoFar++;
        var remaining = Math.Max(0, _requiredOfferings - _offeredSoFar);

        BroadcastToDepartment(component.TargetDepartment,
            "tribal-hunt-popup-progress",
            ("user", uid),
            ("remaining", remaining));

        if (_offeredSoFar < _requiredOfferings)
            return;

        CompleteHunt(component.TargetDepartment);
    }

    private void CompleteHunt(string departmentId)
    {
        _activeHunt = false;

        var expiry = _timing.CurTime + _rewardDuration;
        var query = EntityQueryEnumerator<MovementSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!IsInDepartment(uid, departmentId))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            var buff = EnsureComp<WarcryBuffComponent>(uid);
            buff.SpeedBonus = Math.Max(buff.SpeedBonus, _rewardSpeedBonus);
            if (expiry > buff.ExpiresAt)
                buff.ExpiresAt = expiry;

            Dirty(uid, buff);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        BroadcastToDepartment(departmentId,
            "tribal-hunt-popup-complete",
            ("seconds", MathF.Ceiling((float) _rewardDuration.TotalSeconds)));
    }

    private bool TryGetHeldFood(EntityUid user, out EntityUid food)
    {
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (!HasComp<FoodComponent>(held))
                continue;

            food = held;
            return true;
        }

        food = EntityUid.Invalid;
        return false;
    }

    private bool CanLeadHunt(EntityUid uid, TribalHuntLeaderComponent component)
    {
        if (!IsInDepartment(uid, component.TargetDepartment))
            return false;

        if (component.ActivatorJobs == null || component.ActivatorJobs.Count == 0)
            return true;

        if (!_mind.TryGetMind(uid, out var mindId, out _))
            return false;

        if (!_jobs.MindTryGetJob(mindId, out _, out var prototype))
            return false;

        return component.ActivatorJobs.Contains(prototype.ID);
    }

    private bool IsInDepartment(EntityUid uid, string departmentId)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _))
            return false;

        if (!_jobs.MindTryGetJob(mindId, out _, out var jobPrototype))
            return false;

        return _jobs.TryGetDepartment(jobPrototype.ID, out var department) && department.ID == departmentId;
    }

    private void BroadcastToDepartment(string departmentId, string key, params (string, object)[] args)
    {
        var query = EntityQueryEnumerator<TribalHuntParticipantComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!IsInDepartment(uid, departmentId))
                continue;

            _popup.PopupEntity(Loc.GetString(key, args), uid, uid, PopupType.Small);
        }
    }

    private void OnLegendaryCreatureShutdown(EntityUid uid, LegendaryCreatureComponent component, ComponentShutdown args)
    {
        if (_activeLegendaryCreature == uid)
            _activeLegendaryCreature = null;
    }
}
