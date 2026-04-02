// #Misfits Add - Server-side faction war system.
// Handles GUI form submissions from clients (declare/ceasefire/warjoin) and the admin /warend command.
// Active war state is maintained here and broadcast to all clients on every change.
// Wars go through a 5-minute Pending phase (during which /warjoin is open) before becoming Active.

using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared._Misfits.FactionWar;
using Content.Shared.GameTicking;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.FactionWar;

/// <summary>
/// Manages player-driven faction war declarations, ceasefires, and individual war enlistment.
/// Rules enforced here (all game-logic stays server-side):
///   - One war per faction at a time (blocking both as aggressor and as target).
///   - Only the highest job-weight online member of the declaring faction may act.
///   - Wars enter a 5-minute Pending phase before becoming Active.
///   - During Pending, non-faction players may /warjoin on either side.
///   - /warend is admin-only and ends any specific war immediately.
/// </summary>
public sealed class FactionWarSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager    _adminManager  = default!;
    [Dependency] private readonly IChatManager     _chat          = default!;
    [Dependency] private readonly IConsoleHost     _conHost       = default!;
    [Dependency] private readonly JobSystem        _jobs          = default!;
    [Dependency] private readonly MindSystem       _minds         = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction    = default!;
    [Dependency] private readonly IPlayerManager   _playerManager = default!;
    [Dependency] private readonly IGameTiming      _gameTiming    = default!;

    // ── Constants ──────────────────────────────────────────────────────────

    /// <summary>Minimum elapsed round time before war can be declared.</summary>
    private static readonly TimeSpan WarCooldownAfterRoundStart = TimeSpan.FromMinutes(30);

    /// <summary>How long a war stays in Pending before becoming Active.</summary>
    private static readonly TimeSpan WarPrepDuration = TimeSpan.FromMinutes(5);

    /// <summary>Minimum word count for casus belli.</summary>
    private const int MinCasusBelliWords = 5;

    // ── State ──────────────────────────────────────────────────────────────

    private readonly List<FactionWarEntry> _activeWars = new();
    private TimeSpan _roundStartTime;

    /// <summary>Activation times for pending wars. Key = WarKey(aggressor, target).</summary>
    private readonly Dictionary<string, TimeSpan> _warActivationTimes = new();

    /// <summary>
    /// Individual players who enlisted via /warjoin.
    /// Key = player UserId, Value = (warKey, side faction ID).
    /// </summary>
    private readonly Dictionary<NetUserId, (string WarKey, string Side)> _warParticipants = new();

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize();

        // Admin-only: force-end a war from the server console.
        _conHost.RegisterCommand(
            "warend",
            "Forcibly end an active war between two factions.",
            "warend <aggressorFactionId> <targetFactionId>",
            WarEndCommand);

        // Receive GUI form submissions from clients.
        SubscribeNetworkEvent<FactionWarOpenPanelRequestEvent>(OnPanelRequest);
        SubscribeNetworkEvent<FactionWarDeclareRequestEvent>(OnDeclareRequest);
        SubscribeNetworkEvent<FactionWarCeasefireRequestEvent>(OnCeasefireRequest);

        // Warjoin panel & enlistment.
        SubscribeNetworkEvent<FactionWarJoinPanelRequestEvent>(OnWarJoinPanelRequest);
        SubscribeNetworkEvent<FactionWarJoinRequestEvent>(OnWarJoinRequest);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    // ── Tick: transition Pending → Active ──────────────────────────────────

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_warActivationTimes.Count == 0)
            return;

        var now = _gameTiming.CurTime;
        var activated = new List<FactionWarEntry>();

        foreach (var war in _activeWars)
        {
            if (war.Phase != WarPhase.Pending)
                continue;

            var key = WarKey(war);
            if (!_warActivationTimes.TryGetValue(key, out var activationTime))
                continue;

            if (now < activationTime)
                continue;

            war.Phase = WarPhase.Active;
            _warActivationTimes.Remove(key);
            activated.Add(war);
        }

        if (activated.Count == 0)
            return;

        BroadcastWarState();
        SendPanelDataToAll();

        foreach (var war in activated)
        {
            var aggDisplay = FactionDisplayName(war.AggressorFaction);
            var tgtDisplay = FactionDisplayName(war.TargetFaction);

            _chat.DispatchServerAnnouncement(
                $"WAR HAS BEGUN\n" +
                $"The conflict between {aggDisplay} and {tgtDisplay} is now active!\n" +
                $"(/warjoin) is now closed for this conflict.",
                Color.OrangeRed);
        }
    }

    // ── GUI: Panel data request ─────────────────────────────────────────

    private void OnPanelRequest(FactionWarOpenPanelRequestEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        SendPanelData(player);
    }

    private void SendPanelData(ICommonSession player)
    {
        var data = new FactionWarPanelDataEvent
        {
            ActiveWars = new List<FactionWarEntry>(_activeWars),
            MyFactionDisplay = Loc.GetString("faction-war-no-faction"),
        };

        if (player.AttachedEntity is { } playerEntity)
        {
            if (TryGetWarFaction(playerEntity, out var factionId))
            {
                data.MyFactionId      = factionId;
                data.MyFactionDisplay = FactionDisplayName(factionId);
            }
        }

        // Check 30-minute cooldown.
        var elapsed = _gameTiming.CurTime - _roundStartTime;
        if (elapsed < WarCooldownAfterRoundStart)
        {
            var remaining = WarCooldownAfterRoundStart - elapsed;
            data.StatusMessage = $"War declarations are locked for the first 30 minutes. " +
                                 $"{remaining.Minutes}m {remaining.Seconds}s remaining.";
        }

        // Compute factions already at war.
        var factionsAtWar = new HashSet<string>();
        foreach (var w in _activeWars)
        {
            factionsAtWar.Add(w.AggressorFaction);
            factionsAtWar.Add(w.TargetFaction);
        }

        // Eligible targets: war-capable, not self, not already in a war.
        foreach (var f in FactionWarConfig.WarCapableFactions)
        {
            if (f == data.MyFactionId || factionsAtWar.Contains(f))
                continue;

            data.EligibleTargets.Add(new FactionWarTargetInfo
            {
                Id          = f,
                DisplayName = FactionDisplayName(f),
            });
        }

        data.EligibleTargets.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));

        // Ceasefire targets: wars involving player's faction (both pending and active).
        if (data.MyFactionId != null)
        {
            foreach (var war in _activeWars)
            {
                if (war.AggressorFaction == data.MyFactionId)
                    data.CeasefireTargets.Add(new FactionWarTargetInfo { Id = war.TargetFaction, DisplayName = FactionDisplayName(war.TargetFaction) });
                else if (war.TargetFaction == data.MyFactionId)
                    data.CeasefireTargets.Add(new FactionWarTargetInfo { Id = war.AggressorFaction, DisplayName = FactionDisplayName(war.AggressorFaction) });
            }
        }

        RaiseNetworkEvent(data, player);
    }

    // ── GUI: Declare War ───────────────────────────────────────────────────

    private void OnDeclareRequest(FactionWarDeclareRequestEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;

        if (player.Status != SessionStatus.InGame ||
            player.AttachedEntity is not { } playerEntity)
        {
            SendResult(player, false, "You must be in-game to declare war.");
            return;
        }

        // 30-minute round-start cooldown.
        var elapsed = _gameTiming.CurTime - _roundStartTime;
        if (elapsed < WarCooldownAfterRoundStart)
        {
            var remaining = WarCooldownAfterRoundStart - elapsed;
            SendResult(player, false,
                $"War declarations are locked for the first 30 minutes. {remaining.Minutes}m {remaining.Seconds}s remaining.");
            return;
        }

        var targetFactionId = msg.TargetFaction.Trim();
        var casusBelli      = msg.CasusBelli.Trim();

        if (casusBelli.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < MinCasusBelliWords)
        {
            SendResult(player, false, $"Casus belli must be at least {MinCasusBelliWords} words.");
            return;
        }

        if (!TryGetWarFaction(playerEntity, out var myFactionId))
        {
            SendResult(player, false, "You are not a member of any war-capable faction.");
            return;
        }

        if (!FactionWarConfig.WarCapableFactions.Contains(targetFactionId))
        {
            SendResult(player, false, $"'{targetFactionId}' is not a valid faction.");
            return;
        }

        if (targetFactionId == myFactionId)
        {
            SendResult(player, false, "You cannot declare war on your own faction.");
            return;
        }

        if (IsFactionInWar(myFactionId))
        {
            SendResult(player, false, "Your faction is already at war. Declare a ceasefire first.");
            return;
        }

        if (IsFactionInWar(targetFactionId))
        {
            SendResult(player, false,
                $"{FactionDisplayName(targetFactionId)} is already engaged in a war.");
            return;
        }

        if (!_minds.TryGetMind(playerEntity, out var mindId, out _))
        {
            SendResult(player, false, "You have no mind entity.");
            return;
        }

        if (GetJobWeight(mindId) < GetFactionTopWeight(myFactionId))
        {
            SendResult(player, false,
                "Only the highest-ranking member of your faction currently online can declare war.");
            return;
        }

        var entry = new FactionWarEntry
        {
            AggressorFaction      = myFactionId,
            TargetFaction         = targetFactionId,
            CasusBelli            = casusBelli,
            DeclarerCharacterName = Name(playerEntity),
            DeclarerJobName       = _jobs.MindTryGetJobName(mindId),
            Phase                 = WarPhase.Pending,
        };

        _activeWars.Add(entry);

        // Start the 5-minute preparation timer.
        var warKey = WarKey(entry);
        _warActivationTimes[warKey] = _gameTiming.CurTime + WarPrepDuration;

        BroadcastWarState();
        SendPanelDataToAll();

        var aggressorDisplay = FactionDisplayName(myFactionId);
        var targetDisplay    = FactionDisplayName(targetFactionId);

        _chat.DispatchServerAnnouncement(
            $"WAR DECLARED\n" +
            $"{aggressorDisplay} has declared war on {targetDisplay}!\n" +
            $"Casus Belli: \"{casusBelli}\"\n" +
            $"{entry.DeclarerCharacterName}, {entry.DeclarerJobName}\n\n" +
            $"War begins in 5 minutes. Use (/warjoin) to choose a side.",
            Color.OrangeRed);

        _chat.SendAdminAnnouncement(
            $"[FactionWar] {player.Name} ({entry.DeclarerCharacterName}) declared war:" +
            $" {aggressorDisplay} vs {targetDisplay}. Casus: {casusBelli}");

        SendResult(player, true,
            $"War declared on {targetDisplay}. War begins in 5 minutes. /warjoin is open.");
    }

    // ── GUI: Ceasefire ─────────────────────────────────────────────────────

    private void OnCeasefireRequest(FactionWarCeasefireRequestEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;

        if (player.Status != SessionStatus.InGame ||
            player.AttachedEntity is not { } playerEntity)
        {
            SendResult(player, false, "You must be in-game to declare a ceasefire.");
            return;
        }

        var targetFactionId = msg.TargetFaction.Trim();

        if (!TryGetWarFaction(playerEntity, out var myFactionId))
        {
            SendResult(player, false, "You are not a member of any war-capable faction.");
            return;
        }

        var war = _activeWars.FirstOrDefault(w =>
            (w.AggressorFaction == myFactionId && w.TargetFaction == targetFactionId) ||
            (w.TargetFaction    == myFactionId && w.AggressorFaction == targetFactionId));

        if (war == null)
        {
            SendResult(player, false,
                $"No active war found between your faction and {FactionDisplayName(targetFactionId)}.");
            return;
        }

        if (!_minds.TryGetMind(playerEntity, out var mindId, out _))
        {
            SendResult(player, false, "You have no mind entity.");
            return;
        }

        if (GetJobWeight(mindId) < GetFactionTopWeight(myFactionId))
        {
            SendResult(player, false,
                "Only the highest-ranking member of your faction currently online can declare a ceasefire.");
            return;
        }

        RemoveWar(war);

        var aggressorDisplay = FactionDisplayName(war.AggressorFaction);
        var targetDisplay    = FactionDisplayName(war.TargetFaction);
        var charName         = Name(playerEntity);
        var jobName          = _jobs.MindTryGetJobName(mindId);

        _chat.DispatchServerAnnouncement(
            $"CEASEFIRE\n" +
            $"{aggressorDisplay} and {targetDisplay} have agreed to a ceasefire.\n" +
            $"{charName}, {jobName}",
            Color.SkyBlue);

        _chat.SendAdminAnnouncement(
            $"[FactionWar] {player.Name} ({charName}) called ceasefire:" +
            $" {aggressorDisplay} vs {targetDisplay}");

        SendResult(player, true,
            $"Ceasefire declared. The conflict with {targetDisplay} has ended.");
    }

    // ── GUI: Warjoin panel data ────────────────────────────────────────────

    private void OnWarJoinPanelRequest(FactionWarJoinPanelRequestEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        var data = new FactionWarJoinPanelDataEvent();

        if (player.AttachedEntity is { } playerEntity && TryGetWarFaction(playerEntity, out _))
            data.AlreadyInFaction = true;

        if (_warParticipants.TryGetValue(player.UserId, out var info))
            data.AlreadyJoinedSide = info.Side;

        data.PendingWars = _activeWars.Where(w => w.Phase == WarPhase.Pending).ToList();

        if (data.PendingWars.Count == 0 && !data.AlreadyInFaction && data.AlreadyJoinedSide == null)
            data.StatusMessage = Loc.GetString("faction-war-join-no-pending");

        RaiseNetworkEvent(data, player);
    }

    // ── GUI: Warjoin enlistment ────────────────────────────────────────────

    private void OnWarJoinRequest(FactionWarJoinRequestEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;

        if (player.Status != SessionStatus.InGame ||
            player.AttachedEntity is not { } playerEntity)
        {
            SendJoinResult(player, false, "You must be in-game to join a war.");
            return;
        }

        if (TryGetWarFaction(playerEntity, out var existingFaction))
        {
            SendJoinResult(player, false,
                $"You are already a member of {FactionDisplayName(existingFaction)}. Your faction participates automatically.");
            return;
        }

        if (_warParticipants.ContainsKey(player.UserId))
        {
            SendJoinResult(player, false, "You have already joined a war.");
            return;
        }

        var war = _activeWars.FirstOrDefault(w =>
            w.AggressorFaction == msg.AggressorFaction &&
            w.TargetFaction == msg.TargetFaction &&
            w.Phase == WarPhase.Pending);

        if (war == null)
        {
            SendJoinResult(player, false, "That war is not in a joinable state (must be pending).");
            return;
        }

        if (msg.ChosenSide != war.AggressorFaction && msg.ChosenSide != war.TargetFaction)
        {
            SendJoinResult(player, false, "Invalid side selection.");
            return;
        }

        var warKey = WarKey(war);
        _warParticipants[player.UserId] = (warKey, msg.ChosenSide);
        BroadcastParticipants();

        var sideName = FactionDisplayName(msg.ChosenSide);
        var charName = Name(playerEntity);

        SendJoinResult(player, true,
            $"You have joined the war on the side of {sideName}. You are now KOS to the enemy.");

        _chat.SendAdminAnnouncement(
            $"[FactionWar] {player.Name} ({charName}) joined war " +
            $"{FactionDisplayName(msg.AggressorFaction)} vs {FactionDisplayName(msg.TargetFaction)} " +
            $"on the side of {sideName}");
    }

    // ── /warend (admin only) ───────────────────────────────────────────────

    private void WarEndCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is { } player && !_adminManager.IsAdmin(player))
        {
            shell.WriteError("You must be an admin to use this command.");
            return;
        }

        if (args.Length < 2)
        {
            shell.WriteError("Usage: warend <aggressorFactionId> <targetFactionId>");
            return;
        }

        var aggressorId = args[0].Trim();
        var targetId    = args[1].Trim();

        var war = _activeWars.FirstOrDefault(w =>
            w.AggressorFaction == aggressorId && w.TargetFaction == targetId);

        if (war == null)
        {
            shell.WriteError(
                $"No active war found: {FactionDisplayName(aggressorId)} vs {FactionDisplayName(targetId)}.");
            return;
        }

        RemoveWar(war);

        var adminName        = shell.Player?.Name ?? "Server";
        var aggressorDisplay = FactionDisplayName(aggressorId);
        var targetDisplay    = FactionDisplayName(targetId);

        _chat.DispatchServerAnnouncement(
            $"WAR ENDED BY COMMAND\n" +
            $"The conflict between {aggressorDisplay} and {targetDisplay} has been resolved.",
            Color.LightGray);

        _chat.SendAdminAnnouncement(
            $"[FactionWar] Admin {adminName} ended war: {aggressorDisplay} vs {targetDisplay}");
    }

    // ── Round lifecycle ────────────────────────────────────────────────────

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        _activeWars.Clear();
        _warActivationTimes.Clear();
        _warParticipants.Clear();
        _roundStartTime = _gameTiming.CurTime;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
            return;

        if (_activeWars.Count > 0)
        {
            RaiseNetworkEvent(
                new FactionWarStateUpdatedEvent { ActiveWars = new List<FactionWarEntry>(_activeWars) },
                e.Session);
        }

        if (_warParticipants.Count > 0)
            SendParticipantsTo(e.Session);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes a war and all associated state (timer, participants), then broadcasts.
    /// </summary>
    private void RemoveWar(FactionWarEntry war)
    {
        _activeWars.Remove(war);

        var warKey = WarKey(war);
        _warActivationTimes.Remove(warKey);

        // Remove all participants enlisted for this specific war.
        var toRemove = _warParticipants
            .Where(kvp => kvp.Value.WarKey == warKey)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var userId in toRemove)
            _warParticipants.Remove(userId);

        BroadcastWarState();
        BroadcastParticipants();
        SendPanelDataToAll();
    }

    private void BroadcastWarState()
    {
        RaiseNetworkEvent(
            new FactionWarStateUpdatedEvent { ActiveWars = new List<FactionWarEntry>(_activeWars) },
            Filter.Broadcast());
    }

    private void SendResult(ICommonSession session, bool success, string message)
    {
        RaiseNetworkEvent(
            new FactionWarCommandResultEvent { Success = success, Message = message },
            session);
    }

    private void SendJoinResult(ICommonSession session, bool success, string message)
    {
        RaiseNetworkEvent(
            new FactionWarJoinResultEvent { Success = success, Message = message },
            session);
    }

    private void SendPanelDataToAll()
    {
        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status == SessionStatus.InGame)
                SendPanelData(session);
        }
    }

    /// <summary>Builds a NetEntity → side dictionary from the current participants.</summary>
    private Dictionary<NetEntity, string> BuildParticipantDict()
    {
        var sessionByUserId = new Dictionary<NetUserId, ICommonSession>();
        foreach (var session in _playerManager.Sessions)
            sessionByUserId[session.UserId] = session;

        var dict = new Dictionary<NetEntity, string>();
        foreach (var (userId, (_, side)) in _warParticipants)
        {
            if (!sessionByUserId.TryGetValue(userId, out var session))
                continue;
            if (session.AttachedEntity is not { } entity)
                continue;
            dict[GetNetEntity(entity)] = side;
        }
        return dict;
    }

    private void BroadcastParticipants()
    {
        var dict = BuildParticipantDict();
        RaiseNetworkEvent(
            new FactionWarParticipantsUpdatedEvent { Participants = dict },
            Filter.Broadcast());
    }

    private void SendParticipantsTo(ICommonSession session)
    {
        var dict = BuildParticipantDict();
        RaiseNetworkEvent(
            new FactionWarParticipantsUpdatedEvent { Participants = dict },
            session);
    }

    private bool IsFactionInWar(string factionId) =>
        _activeWars.Any(w => w.AggressorFaction == factionId || w.TargetFaction == factionId);

    private bool TryGetWarFaction(EntityUid entity, out string factionId)
    {
        factionId = string.Empty;
        // Check all NPC faction IDs (including aliases like Rangers) and resolve to canonical war faction.
        foreach (var f in FactionWarConfig.AllWarFactionIds)
        {
            if (_npcFaction.IsMember(entity, f))
            {
                factionId = FactionWarConfig.ResolveWarFaction(f);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the highest job weight among online members of a war faction,
    /// including members of any alias factions (e.g. Rangers when factionId is NCR).
    /// </summary>
    private int GetFactionTopWeight(string factionId)
    {
        // Build the set of NPC faction IDs to check (canonical + any aliases).
        var factionIds = new List<string> { factionId };
        foreach (var (raw, canonical) in FactionWarConfig.FactionAliases)
        {
            if (canonical == factionId)
                factionIds.Add(raw);
        }

        var top = 0;
        var query = EntityQueryEnumerator<NpcFactionMemberComponent, ActorComponent>();
        while (query.MoveNext(out var entity, out _, out var actor))
        {
            if (actor.PlayerSession.Status != SessionStatus.InGame)
                continue;

            var isMember = false;
            foreach (var fid in factionIds)
            {
                if (_npcFaction.IsMember(entity, fid))
                {
                    isMember = true;
                    break;
                }
            }
            if (!isMember)
                continue;

            if (!_minds.TryGetMind(entity, out var mindId, out _))
                continue;
            var w = GetJobWeight(mindId);
            if (w > top)
                top = w;
        }
        return top;
    }

    private int GetJobWeight(EntityUid mindId) =>
        _jobs.MindTryGetJob(mindId, out _, out var proto) ? proto.Weight : 0;

    public static string FactionDisplayName(string factionId) =>
        FactionWarConfig.FactionDisplayName(factionId);

    private static string WarKey(FactionWarEntry war) =>
        $"{war.AggressorFaction}|{war.TargetFaction}";
}
