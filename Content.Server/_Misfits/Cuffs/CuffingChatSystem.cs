// #Misfits Add: Broadcasts chat emotes at each observable stage of cuffing/uncuffing (start and success).
using Content.Server.Chat.Systems;
using Content.Shared._Misfits.Cuffs;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;

namespace Content.Server._Misfits.Cuffs;

/// <summary>
/// Hooks <see cref="CuffStartedEvent"/>, <see cref="CuffAppliedEvent"/>, and <see cref="UncuffStartedEvent"/>
/// to send local-area emote chat messages so nearby players see cuffing actions in the Emotes channel
/// rather than as sprite-only popups.
/// </summary>
public sealed class CuffingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Fires when a cuffing do-after begins (user starts trying to cuff).
        SubscribeLocalEvent<CuffStartedEvent>(OnCuffStarted);

        // Fires when cuffs are successfully applied.
        SubscribeLocalEvent<CuffAppliedEvent>(OnCuffApplied);

        // Fires when an uncuffing do-after begins (user starts trying to remove cuffs).
        SubscribeLocalEvent<UncuffStartedEvent>(OnUncuffStarted);
    }

    private void OnCuffStarted(ref CuffStartedEvent ev)
    {
        var targetName = Identity.Entity(ev.Target, EntityManager);

        var message = ev.User == ev.Target
            ? Loc.GetString("misfits-chat-cuff-start-self")
            : Loc.GetString("misfits-chat-cuff-start", ("target", targetName));

        _chat.TrySendInGameICMessage(ev.User, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }

    /// <summary>
    /// Triggered after cuffs are successfully applied; sends an emote chat message to nearby players.
    /// </summary>
    private void OnCuffApplied(ref CuffAppliedEvent ev)
    {
        var targetName = Identity.Entity(ev.Target, EntityManager);

        var message = ev.User == ev.Target
            ? Loc.GetString("misfits-chat-cuff-self")
            : Loc.GetString("misfits-chat-cuff-applied", ("target", targetName));

        _chat.TrySendInGameICMessage(ev.User, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }

    private void OnUncuffStarted(ref UncuffStartedEvent ev)
    {
        var targetName = Identity.Entity(ev.Target, EntityManager);

        var message = ev.User == ev.Target
            ? Loc.GetString("misfits-chat-uncuff-start-self")
            : Loc.GetString("misfits-chat-uncuff-start", ("target", targetName));

        _chat.TrySendInGameICMessage(ev.User, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }
}
