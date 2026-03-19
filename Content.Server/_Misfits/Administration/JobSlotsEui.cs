// #Misfits Change - Server-side EUI for the Job Slots admin panel
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared._Misfits.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.Administration;

/// <summary>
/// Server-side EUI that exposes per-station job slot counts to admins and lets
/// them increment or decrement individual job slots without needing to select a
/// player first.
/// </summary>
public sealed class JobSlotsEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private readonly ISawmill _sawmill;
    private StationJobsSystem StationJobs => _entManager.System<StationJobsSystem>();
    private StationSystem StationSystem => _entManager.System<StationSystem>();

    // Used for station jobs that are not assigned to any department prototype.
    private const string UncategorizedDepartmentId = "Uncategorized";

    private EntityUid? _station;
    private string? _stationName;

    public JobSlotsEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("admin.job_slots_eui");
    }

    public override void Opened()
    {
        base.Opened();
        RefreshStation();
        StateDirty();
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private void RefreshStation()
    {
        if (Player.AttachedEntity is { Valid: true } attached &&
            StationSystem.GetOwningStation(attached) is { } owning)
        {
            _station = owning;
        }
        else
        {
            var stations = StationSystem.GetStations();
            _station = stations.Count > 0 ? stations[0] : null;
        }

        if (_station is { } uid &&
            _entManager.TryGetComponent<MetaDataComponent>(uid, out var meta))
        {
            _stationName = meta.EntityName;
        }
        else
        {
            _stationName = null;
        }
    }

    public override EuiStateBase GetNewState()
    {
        var canManage = _admin.HasAdminFlag(Player, AdminFlags.Admin);
        var depts = new List<JobSlotDepartmentInfo>();

        if (_station == null)
            return new JobSlotsEuiState(_stationName, canManage, depts);

        var stationJobs = StationJobs.GetJobs(_station.Value);
        var stationJobIds = new HashSet<string>(stationJobs.Keys.Where(id => _proto.HasIndex<JobPrototype>(id)));

        // Build a job -> departments map from prototypes, then only display jobs
        // that exist on the current station (map-specific role config).
        var jobsByDepartment = new Dictionary<string, List<ProtoId<JobPrototype>>>();

        foreach (var dept in _proto.EnumeratePrototypes<DepartmentPrototype>()
                     .OrderByDescending(d => d.Weight)
                     .ThenBy(d => d.ID))
        {
            var sortedRoles = dept.Roles
                .Where(id => stationJobIds.Contains(id))
                .Select(id => (id, proto: _proto.Index<JobPrototype>(id)))
                .OrderByDescending(t => t.proto.RealDisplayWeight)
                .ThenBy(t => t.proto.ID)
                .Select(t => t.id)
                .ToList();

            if (sortedRoles.Count == 0)
                continue;

            jobsByDepartment[dept.ID] = sortedRoles;
        }

        // Include station jobs that do not appear in any department prototype.
        var assignedJobs = new HashSet<string>(jobsByDepartment
            .SelectMany(x => x.Value)
            .Select(id => (string) id));
        var uncategorized = stationJobIds
            .Where(id => !assignedJobs.Contains(id))
            .Select(id => (id, proto: _proto.Index<JobPrototype>(id)))
            .OrderByDescending(t => t.proto.RealDisplayWeight)
            .ThenBy(t => t.proto.ID)
            .Select(t => (ProtoId<JobPrototype>) t.id)
            .ToList();

        if (uncategorized.Count > 0)
            jobsByDepartment[UncategorizedDepartmentId] = uncategorized;

        var orderedDepartmentIds = jobsByDepartment.Keys
            .OrderByDescending(GetDepartmentWeight)
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToList();

        foreach (var deptId in orderedDepartmentIds)
        {
            var sortedRoles = jobsByDepartment[deptId];

            var jobs = new List<JobSlotInfo>();
            foreach (var jobId in sortedRoles)
            {
                int? slots = null;
                var hasCfg = false;

                if (StationJobs.TryGetJobSlot(_station.Value, jobId, out var slotData))
                {
                    hasCfg = true;
                    slots = slotData is null ? null : (int) slotData.Value;
                }

                jobs.Add(new JobSlotInfo(jobId, slots, hasCfg));
            }

            if (jobs.Count == 0)
                continue;

            depts.Add(new JobSlotDepartmentInfo(deptId, jobs));
        }

        return new JobSlotsEuiState(_stationName, canManage, depts);
    }

    private int GetDepartmentWeight(string departmentId)
    {
        if (_proto.TryIndex<DepartmentPrototype>(departmentId, out var dept))
            return dept.Weight;

        // Keep synthetic / unknown groups after real departments.
        return int.MinValue;
    }

    // -------------------------------------------------------------------------
    // Message handling
    // -------------------------------------------------------------------------

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        // #Misfits Change - Whitelist flag not universally assigned; gate on Admin instead.
        if (!_admin.HasAdminFlag(Player, AdminFlags.Admin))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to use job slots EUI without Admin flag");
            return;
        }

        if (msg is not AdjustJobSlotsMessage adjust)
            return;

        HandleAdjust(adjust.Job, adjust.Delta);
    }

    private void HandleAdjust(ProtoId<JobPrototype> job, int delta)
    {
        if (_station == null || !_proto.HasIndex<JobPrototype>(job) || delta == 0)
            return;

        if (!StationJobs.TryAdjustJobSlot(_station.Value, job, delta, createSlot: true, clamp: true))
            return;

        _sawmill.Info($"{Player.Name} ({Player.UserId}) adjusted slots for {job} by {delta:+#;-#} on station {_stationName ?? _station.Value.ToString()}");
        StateDirty();
    }
}
