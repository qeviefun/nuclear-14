// #Misfits Change - Server-side EUI for the Whitelist Search admin panel
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Players.JobWhitelist;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Misfits.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Server._Misfits.Administration;

public sealed class WhitelistSearchEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;

    private StationJobsSystem _stationJobs = default!;
    private StationSystem _stationSystem = default!;

    private readonly ISawmill _sawmill;
    private readonly WhitelistSearchMode _mode;

    private static readonly HashSet<string> AllowedDepartments = new()
    {
        "BrotherhoodOfSteel",
        "CaesarLegion",
        "NCR",
        "Townsfolk",
        "FEVMutants",
        "Tribe",
        "Robots",
        "Vault",
        "Raider",
    };

    private List<WhitelistPlayerInfo> _searchResults = new();
    private NetUserId? _selectedPlayerId;
    private string? _selectedPlayerName;
    private HashSet<ProtoId<JobPrototype>>? _whitelists;
    private List<WhitelistJobAdminInfo>? _jobAdminInfo;
    private EntityUid? _selectedStation;
    private string? _selectedStationName;
    private List<EntityUid> _selectedStations = new();

    public WhitelistSearchEui(WhitelistSearchMode mode = WhitelistSearchMode.RoleWhitelists)
    {
        IoCManager.InjectDependencies(this);
        _stationJobs = _sysMan.GetEntitySystem<StationJobsSystem>();
        _stationSystem = _sysMan.GetEntitySystem<StationSystem>();
        _sawmill = _log.GetSawmill("admin.whitelist_search_eui");
        _mode = mode;
    }

    public override void Opened()
    {
        base.Opened();
        if (_mode == WhitelistSearchMode.JobSlots)
            InitJobSlots();
    }

    private void InitJobSlots()
    {
        _selectedStations = ResolveTargetStations();
        _selectedStationName = _selectedStations.Count > 0
            ? Loc.GetString("misfits-whitelist-search-station-all", ("count", _selectedStations.Count))
            : null;
        _whitelists = new HashSet<ProtoId<JobPrototype>>();
        _jobAdminInfo = BuildJobSlotInfoSync();
        StateDirty();
    }

    private List<WhitelistJobAdminInfo> BuildJobSlotInfoSync()
    {
        var info = new List<WhitelistJobAdminInfo>();
        foreach (var department in _proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!AllowedDepartments.Contains(department.ID))
                continue;

            foreach (var jobId in department.Roles)
            {
                if (!_proto.TryIndex(jobId, out _))
                    continue;

                var (slots, hasSlotConfig) = GetAggregateJobSlots(jobId, _selectedStations);
                info.Add(new WhitelistJobAdminInfo(jobId, TimeSpan.Zero, slots, hasSlotConfig));
            }
        }
        return info;
    }

    public override EuiStateBase GetNewState()
    {
        return new WhitelistSearchEuiState(
            _mode,
            _admin.HasAdminFlag(Player, AdminFlags.Admin),
            _admin.HasAdminFlag(Player, AdminFlags.Admin),
            _searchResults,
            _selectedPlayerName,
            _selectedPlayerId,
            _whitelists,
            _jobAdminInfo,
            _selectedStationName);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        _sawmill.Debug($"Received EUI message of type: {msg.GetType().Name}");

        try
        {
            if (!_admin.HasAdminFlag(Player, AdminFlags.Whitelist))
            {
                _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to use whitelist search without permission");
                return;
            }

            switch (msg)
            {
                case SearchPlayersMessage search:
                    _sawmill.Debug($"Handling search for: '{search.Query}'");
                    await HandleSearch(search.Query);
                    break;
                case SelectPlayerMessage select:
                    await HandleSelectPlayer(select.PlayerId);
                    break;
                case SetWhitelistSearchJobMessage setJob:
                    HandleSetJob(setJob.Job, setJob.Whitelisting);
                    break;
                case AddWhitelistSearchRoleTimeMessage addTime:
                    await HandleAddRoleTime(addTime.Job, addTime.TimeString);
                    break;
                case AdjustWhitelistSearchJobSlotsMessage adjustSlots:
                    HandleAdjustJobSlots(adjustSlots.Job, adjustSlots.Delta);
                    break;
                default:
                    _sawmill.Debug($"Unknown message type: {msg.GetType().FullName}");
                    break;
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error handling EUI message: {e}");
        }
    }

    private async Task HandleSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            _searchResults = new List<WhitelistPlayerInfo>();
            StateDirty();
            return;
        }

        try
        {
            var records = await _db.SearchPlayersByName(query);
            _sawmill.Debug($"Search for '{query}' returned {records.Count} results");
            _searchResults = records
                .Select(r => new WhitelistPlayerInfo(r.UserId, r.LastSeenUserName))
                .ToList();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error searching players by name '{query}': {e}");
            _searchResults = new List<WhitelistPlayerInfo>();
        }

        StateDirty();
    }

    private async Task HandleSelectPlayer(NetUserId playerId)
    {
        var record = await _db.GetPlayerRecordByUserId(playerId);
        if (record == null)
        {
            _sawmill.Warning($"Admin {Player.Name} tried to select non-existent player {playerId}");
            return;
        }

        _selectedPlayerId = playerId;
        _selectedPlayerName = record.LastSeenUserName;

        _whitelists = new HashSet<ProtoId<JobPrototype>>();
        var jobs = await _db.GetJobWhitelists(playerId.UserId);
        foreach (var id in jobs)
        {
            if (_proto.HasIndex<JobPrototype>(id))
                _whitelists.Add(id);
        }

        StationJobsComponent? stationJobs = null;

        if (_mode == WhitelistSearchMode.JobSlots)
        {
            _selectedStations = ResolveTargetStations();
            _selectedStation = _selectedStations.FirstOrDefault();
            _selectedStationName = _selectedStations.Count == 0
                ? null
                : Loc.GetString("misfits-whitelist-search-station-all", ("count", _selectedStations.Count));
        }
        else
        {
            _selectedStation = ResolveTargetStation(out stationJobs);
            if (_selectedStation is { } resolvedStation)
            {
                _selectedStations = new List<EntityUid> { resolvedStation };
                _selectedStationName = GetStationName(resolvedStation);
            }
            else
            {
                _selectedStations = new List<EntityUid>();
                _selectedStationName = null;
            }
        }

        _jobAdminInfo = await BuildJobAdminInfo(playerId, stationJobs);

        StateDirty();
    }

    private void HandleSetJob(ProtoId<JobPrototype> job, bool whitelisting)
    {
        if (_mode != WhitelistSearchMode.RoleWhitelists)
            return;

        if (_selectedPlayerId == null || _whitelists == null)
            return;

        if (!_proto.HasIndex<JobPrototype>(job))
            return;

        if (whitelisting)
        {
            _jobWhitelist.AddWhitelist(_selectedPlayerId.Value, job);
            _whitelists.Add(job);
        }
        else
        {
            _jobWhitelist.RemoveWhitelist(_selectedPlayerId.Value, job);
            _whitelists.Remove(job);
        }

        var verb = whitelisting ? "added" : "removed";
        _sawmill.Info($"{Player.Name} ({Player.UserId}) {verb} whitelist for {job} to player {_selectedPlayerName} ({_selectedPlayerId.Value.UserId})");

        StateDirty();
    }

    private async Task HandleAddRoleTime(ProtoId<JobPrototype> job, string timeString)
    {
        if (_mode != WhitelistSearchMode.RoleWhitelists)
            return;

        if (!_admin.HasAdminFlag(Player, AdminFlags.Admin))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to add role time without permission");
            return;
        }

        if (_selectedPlayerId == null || !_proto.TryIndex(job, out var jobProto))
            return;

        var minutes = Content.Server.Administration.Commands.PlayTimeCommandUtilities.CountMinutes(timeString);
        if (minutes <= 0)
            return;

        var delta = TimeSpan.FromMinutes(minutes);
        TimeSpan updatedTime;

        if (_player.TryGetSessionById(_selectedPlayerId.Value, out var session))
        {
            _playTime.FlushTracker(session);
            _playTime.AddTimeToTracker(session, jobProto.PlayTimeTracker, delta);
            _playTime.SaveSession(session);
            updatedTime = _playTime.GetPlayTimeForTracker(session, jobProto.PlayTimeTracker);
        }
        else
        {
            var playTimes = await _db.GetPlayTimes(_selectedPlayerId.Value.UserId);
            var currentTime = playTimes
                .Where(x => x.Tracker == jobProto.PlayTimeTracker)
                .Select(x => x.TimeSpent)
                .FirstOrDefault();

            updatedTime = currentTime + delta;
            await _db.UpdatePlayTimes(new[]
            {
                new PlayTimeUpdate(_selectedPlayerId.Value, jobProto.PlayTimeTracker, updatedTime),
            });
        }

        if (_jobAdminInfo != null)
        {
            var row = _jobAdminInfo.FirstOrDefault(x => x.Job == job);
            if (row != null)
                row.RoleTime = updatedTime;
        }

        _sawmill.Info($"{Player.Name} ({Player.UserId}) added {delta} of role time for {job} to player {_selectedPlayerName} ({_selectedPlayerId.Value.UserId})");
        StateDirty();
    }

    private void HandleAdjustJobSlots(ProtoId<JobPrototype> job, int delta)
    {
        if (_mode != WhitelistSearchMode.JobSlots)
            return;

        if (!_admin.HasAdminFlag(Player, AdminFlags.Admin))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to adjust job slots without permission");
            return;
        }

        // Re-resolve stations in case of a round restart since the panel was opened.
        _selectedStations = ResolveTargetStations();
        _selectedStationName = _selectedStations.Count > 0
            ? Loc.GetString("misfits-whitelist-search-station-all", ("count", _selectedStations.Count))
            : null;

        if (!_proto.HasIndex<JobPrototype>(job)
            || delta == 0
            || _selectedStations.Count == 0)
            return;

        var adjustedAny = false;

        foreach (var station in _selectedStations)
        {
            if (!_entManager.TryGetComponent(station, out StationJobsComponent? stationJobs))
                continue;

            if (_stationJobs.TryAdjustJobSlot(station, job, delta, createSlot: true, clamp: true, stationJobs))
                adjustedAny = true;
        }

        if (!adjustedAny)
            return;

        if (_jobAdminInfo != null)
        {
            var row = _jobAdminInfo.FirstOrDefault(x => x.Job == job);
            if (row != null)
            {
                var aggregate = GetAggregateJobSlots(job, _selectedStations);
                row.HasSlotConfiguration = aggregate.hasSlotConfiguration;
                row.Slots = aggregate.slots;
            }
        }

        _sawmill.Info($"{Player.Name} ({Player.UserId}) adjusted slots for {job} by {delta} across {_selectedStations.Count} station(s)");
        StateDirty();
    }

    private async Task<List<WhitelistJobAdminInfo>> BuildJobAdminInfo(NetUserId playerId, StationJobsComponent? stationJobs)
    {
        var playTimeDict = new Dictionary<string, TimeSpan>();
        var playTimes = await _db.GetPlayTimes(playerId.UserId);

        foreach (var playTime in playTimes)
        {
            playTimeDict[playTime.Tracker] = playTime.TimeSpent;
        }

        var info = new List<WhitelistJobAdminInfo>();

        foreach (var department in _proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!AllowedDepartments.Contains(department.ID))
                continue;

            foreach (var jobId in department.Roles)
            {
                if (!_proto.TryIndex(jobId, out var job))
                    continue;

                var roleTime = playTimeDict.GetValueOrDefault(job.PlayTimeTracker);
                var hasSlotConfiguration = false;
                int? slots = 0;

                if (_mode == WhitelistSearchMode.JobSlots)
                {
                    var aggregate = GetAggregateJobSlots(jobId, _selectedStations);
                    hasSlotConfiguration = aggregate.hasSlotConfiguration;
                    slots = aggregate.slots;
                }
                else if (_selectedStation != null
                    && stationJobs != null
                    && _stationJobs.TryGetJobSlot(_selectedStation.Value, jobId, out var slotData, stationJobs))
                {
                    hasSlotConfiguration = true;
                    slots = slotData is null ? null : (int) slotData.Value;
                }

                info.Add(new WhitelistJobAdminInfo(jobId, roleTime, slots, hasSlotConfiguration));
            }
        }

        return info;
    }

    private EntityUid? ResolveTargetStation(out StationJobsComponent? stationJobs)
    {
        stationJobs = null;

        if (Player.AttachedEntity is { Valid: true } attached &&
            _stationSystem.GetOwningStation(attached) is { } currentStation &&
            _entManager.TryGetComponent(currentStation, out stationJobs))
        {
            return currentStation;
        }

        foreach (var station in _stationSystem.GetStations())
        {
            if (_entManager.TryGetComponent(station, out stationJobs))
                return station;
        }

        stationJobs = null;
        return null;
    }

    private List<EntityUid> ResolveTargetStations()
    {
        var stations = new List<EntityUid>();

        foreach (var station in _stationSystem.GetStations())
        {
            if (_entManager.HasComponent<StationJobsComponent>(station))
                stations.Add(station);
        }

        return stations;
    }

    private (int? slots, bool hasSlotConfiguration) GetAggregateJobSlots(ProtoId<JobPrototype> job, IReadOnlyList<EntityUid> stations)
    {
        var hasSlotConfiguration = false;
        var totalSlots = 0;
        var hasUnlimitedSlots = false;

        foreach (var station in stations)
        {
            if (!_entManager.TryGetComponent(station, out StationJobsComponent? stationJobs))
                continue;

            if (!_stationJobs.TryGetJobSlot(station, job, out var slotData, stationJobs))
                continue;

            hasSlotConfiguration = true;

            if (slotData == null)
            {
                hasUnlimitedSlots = true;
                continue;
            }

            totalSlots += (int) slotData.Value;
        }

        if (!hasSlotConfiguration)
            return (0, false);

        return hasUnlimitedSlots ? (null, true) : (totalSlots, true);
    }

    private string GetStationName(EntityUid station)
    {
        return _entManager.TryGetComponent(station, out MetaDataComponent? meta)
            ? meta.EntityName
            : station.ToString();
    }
}
