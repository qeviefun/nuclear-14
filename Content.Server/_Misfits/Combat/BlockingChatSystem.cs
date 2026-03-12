// #Misfits Add: Broadcasts emote chat messages to nearby players when a player raises or lowers their shield.
// Replaces the observer-visible PopupEntity calls that were commented out of Content.Shared/Blocking/BlockingSystem.cs.
using Content.Server.Chat.Systems;
using Content.Shared.Blocking;
using Content.Shared.Chat;

namespace Content.Server._Misfits.Combat;

/// <summary>
/// Hooks <see cref="BlockingUserComponent"/> lifecycle events to send local-area emote chat messages
/// so nearby players see "* Name raises their shield *" / "* Name lowers their shield *" in the emote
/// channel instead of floating sprite popups.
/// </summary>
public sealed class BlockingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        // BlockingUserComponent is dynamically added/removed by BlockingSystem when blocking starts/stops.
        SubscribeLocalEvent<BlockingUserComponent, ComponentStartup>(OnBlockingStarted);
        SubscribeLocalEvent<BlockingUserComponent, ComponentShutdown>(OnBlockingStopped);
    }

    private void OnBlockingStarted(EntityUid uid, BlockingUserComponent comp, ComponentStartup args)
    {
        // Resolve the shield's display name; fall back to a generic string if the item is gone.
        var shieldName = comp.BlockingItem.HasValue && Exists(comp.BlockingItem.Value)
            ? Name(comp.BlockingItem.Value)
            : "shield";

        var message = Loc.GetString("misfits-chat-blocking-start", ("shield", shieldName));

        // Send as a local-area emote from the blocker — visible to nearby players in the Emotes channel.
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }

    private void OnBlockingStopped(EntityUid uid, BlockingUserComponent comp, ComponentShutdown args)
    {
        // Skip if the entity itself is being deleted; sending chat from a terminating entity causes errors.
        if (TerminatingOrDeleted(uid))
            return;

        var shieldName = comp.BlockingItem.HasValue && Exists(comp.BlockingItem.Value)
            ? Name(comp.BlockingItem.Value)
            : "shield";

        var message = Loc.GetString("misfits-chat-blocking-stop", ("shield", shieldName));

        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }
}
