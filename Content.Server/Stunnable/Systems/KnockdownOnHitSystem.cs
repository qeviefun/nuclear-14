using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Server.Stunnable.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Stunnable.Systems;

public sealed class KnockdownOnHitSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockdownOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<KnockdownOnHitComponent> entity, ref MeleeHitEvent args)
    {
        if (args.Direction.HasValue || !args.IsHit || !args.HitEntities.Any() || entity.Comp.Duration <= TimeSpan.Zero)
            return;

        var ev = new KnockdownOnHitAttemptEvent();
        RaiseLocalEvent(entity, ref ev);
        if (ev.Cancelled)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!TryComp(target, out StatusEffectsComponent? statusEffects))
                continue;

            // #Misfits Change Add: broadcast only when the knockdown actually lands.
            if (!_stun.TryKnockdown(target,
                    entity.Comp.Duration,
                    entity.Comp.RefreshDuration,
                    entity.Comp.DropHeldItemsBehavior,
                    statusEffects))
                continue;

            if (!HasComp<MobStateComponent>(target))
                continue;

            var targetName = Identity.Entity(target, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);

            _chat.TrySendInGameICMessage(args.User,
                Loc.GetString("misfits-chat-knockdown-hit", ("target", targetName)),
                InGameICChatType.Emote,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);

            _chat.TrySendInGameICMessage(target,
                Loc.GetString("misfits-chat-knockdown-hit-victim", ("user", userName)),
                InGameICChatType.Emote,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        }
    }
}
