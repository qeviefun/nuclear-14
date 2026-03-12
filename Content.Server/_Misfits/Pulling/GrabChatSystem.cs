// #Misfits Add: Broadcasts a chat emote and plays a grab sound when a mob entity is pulled/grabbed by another entity.
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Misfits.Pulling;

/// <summary>
/// Hooks <see cref="PullStartedMessage"/> to broadcast a local-area emote chat message and
/// play a fabric-grab sound whenever a mob entity is grabbed/pulled by another entity.
/// Only fires when the pulled entity is a mob (has <see cref="MobStateComponent"/>) — dragging
/// crates or other objects will not produce the emote.
/// </summary>
public sealed class GrabChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // Sound played at the puller's position when a grab starts — a cloth rustle
    // to represent seizing someone's arm / collar.
    private static readonly SoundPathSpecifier GrabSound =
        new SoundPathSpecifier("/Audio/Items/Handling/cloth_pickup.ogg");

    public override void Initialize()
    {
        base.Initialize();

        // PullStartedMessage is raised on the pullable entity; we only care when
        // the thing being pulled is an actual mob (player, NPC, etc.).
        SubscribeLocalEvent<PullableComponent, PullStartedMessage>(OnPullStarted);
    }

    /// <summary>
    /// Fired on the pullable entity when a pull begins.
    /// Sends an emote line from the puller and plays a grab sound.
    /// </summary>
    private void OnPullStarted(EntityUid uid, PullableComponent component, PullStartedMessage args)
    {
        // Only fire for mob entities — skip items, machinery, corpse-less objects.
        if (!HasComp<MobStateComponent>(uid))
            return;

        // Don't fire if the puller and pullable are the same entity.
        if (args.PullerUid == uid)
            return;

        // Build the emote message. Identity.Entity respects disguises/name masks.
        var pulledName = Identity.Entity(uid, EntityManager);
        var message = Loc.GetString("misfits-chat-grab-start", ("grabbed", pulledName));

        // Broadcast as an emote from the puller so nearby players see it in the Emotes channel.
        _chat.TrySendInGameICMessage(args.PullerUid, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);

        // Play a short cloth/fabric rustle at the puller to reinforce the physical action.
        _audio.PlayPvs(GrabSound, args.PullerUid);
    }
}
