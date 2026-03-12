// #Misfits Add: broadcast local emotes when one player straps or frees another.
using Content.Server.Chat.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared._Misfits.Buckle.Events;

namespace Content.Server._Misfits.Buckle;

public sealed class BuckleChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrapComponent, ActorStrappedEvent>(OnActorStrapped);
        SubscribeLocalEvent<StrapComponent, ActorUnstrappedEvent>(OnActorUnstrapped);
    }

    private void OnActorStrapped(Entity<StrapComponent> ent, ref ActorStrappedEvent args)
    {
        if (args.User is not { } user || user == args.Buckle.Owner)
            return;

        var targetName = Identity.Entity(args.Buckle.Owner, EntityManager);
        var strapName = Identity.Entity(ent.Owner, EntityManager);
        var userName = Identity.Entity(user, EntityManager);

        _chat.TrySendInGameICMessage(user,
            Loc.GetString("misfits-chat-buckle-strap", ("target", targetName), ("strap", strapName)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);

        _chat.TrySendInGameICMessage(args.Buckle.Owner,
            Loc.GetString("misfits-chat-buckle-victim", ("user", userName), ("strap", strapName)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);
    }

    private void OnActorUnstrapped(Entity<StrapComponent> ent, ref ActorUnstrappedEvent args)
    {
        if (args.User is not { } user || user == args.Buckle.Owner)
            return;

        var targetName = Identity.Entity(args.Buckle.Owner, EntityManager);
        var strapName = Identity.Entity(ent.Owner, EntityManager);
        var userName = Identity.Entity(user, EntityManager);

        _chat.TrySendInGameICMessage(user,
            Loc.GetString("misfits-chat-unbuckle-release", ("target", targetName), ("strap", strapName)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);

        _chat.TrySendInGameICMessage(args.Buckle.Owner,
            Loc.GetString("misfits-chat-unbuckle-victim", ("user", userName), ("strap", strapName)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);
    }
}