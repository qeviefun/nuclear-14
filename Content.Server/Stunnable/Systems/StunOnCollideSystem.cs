using Content.Server.Stunnable.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Server.Stunnable.Systems;

[UsedImplicitly]
internal sealed class StunOnCollideSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StunOnCollideComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<StunOnCollideComponent, ThrowDoHitEvent>(HandleThrow);
    }

    private void TryDoCollideStun(Entity<StunOnCollideComponent> ent, EntityUid target, EntityUid? user = null)
    {
        if (!EntityManager.TryGetComponent<StatusEffectsComponent>(target, out var status) ||
            ent.Comp.Blacklist is { } blacklist && _entityWhitelist.IsValid(blacklist, target))
            return;

        _stunSystem.TryStun(target, ent.Comp.StunAmount, true, status);
        var knockedDown = _stunSystem.TryKnockdown(target, ent.Comp.KnockdownAmount, true, status);

        _stunSystem.TrySlowdown(
            target,
            ent.Comp.SlowdownAmount,
            true,
            ent.Comp.WalkSpeedMultiplier,
            ent.Comp.RunSpeedMultiplier,
            status);

        // #Misfits Change Add: only broadcast when an attributed collision actually knocks a mob down.
        if (!knockedDown || user is not { } attacker || !HasComp<MobStateComponent>(target))
            return;

        var targetName = Identity.Entity(target, EntityManager);
        var userName = Identity.Entity(attacker, EntityManager);

        _chat.TrySendInGameICMessage(attacker,
            Loc.GetString("misfits-chat-collision-knockdown", ("target", targetName)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);

        _chat.TrySendInGameICMessage(target,
            Loc.GetString("misfits-chat-collision-knockdown-victim", ("user", userName)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true);
    }

    private void HandleCollide(Entity<StunOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        TryDoCollideStun(ent, args.OtherEntity);
    }

    private void HandleThrow(Entity<StunOnCollideComponent> ent, ref ThrowDoHitEvent args) =>
        TryDoCollideStun(ent, args.Target, args.Component.Thrower);
}
