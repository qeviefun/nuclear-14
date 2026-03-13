// #Misfits Add - Aggro warning system for NPC mobs.
// When a mob first detects a hostile target it plays a ping and waits 2 seconds
// before committing to attack. If the target retreats beyond vision range
// during the delay, the mob de-aggros. If the target steps inside
// InstantAggroRange (5 tiles) the delay is skipped immediately.
using Content.Server._Misfits.NPC.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.NPC.Systems;

public sealed class AggroWarningSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Same combat-toggle ping used by the player combat-mode system.
    private const string WarningPingSound = "/Audio/Effects/toggleoncombat.ogg";

    // Audible range for the mob's warning ping (tiles).
    private const float PingMaxDistance = 10f;

    public override void Initialize()
    {
        base.Initialize();

        // When the NPC enters melee or ranged combat, attach a warning window.
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentStartup>(OnCombatStartup);
        SubscribeLocalEvent<NPCRangedCombatComponent, ComponentStartup>(OnCombatStartup);

        // Clean up warning if combat components are removed before the delay expires.
        SubscribeLocalEvent<AggroWarningComponent, ComponentShutdown>(OnWarningShutdown);
    }

    /// <summary>
    /// Shared handler for both melee and ranged combat component startups.
    /// Adds the AggroWarningComponent if one doesn't already exist and the NPC is an HTN-driven mob.
    /// </summary>
    private void OnCombatStartup<T>(EntityUid uid, T comp, ComponentStartup args) where T : Component
    {
        // Only apply to actual HTN-controlled NPCs, not players.
        if (!HasComp<HTNComponent>(uid))
            return;

        // Don't double-add if already in a warning window (e.g. melee+ranged both started).
        if (HasComp<AggroWarningComponent>(uid))
            return;

        var warning = EnsureComp<AggroWarningComponent>(uid);
        warning.TimeRemaining = 2f;
        warning.PingPlayed = false;
    }

    private void OnWarningShutdown(EntityUid uid, AggroWarningComponent comp, ComponentShutdown args)
    {
        // Nothing to clean up — the component is self-contained.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AggroWarningComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var warning, out var xform))
        {
            // Play the warning ping once at the start of the window.
            if (!warning.PingPlayed)
            {
                warning.PingPlayed = true;
                _audio.PlayPvs(
                    new SoundPathSpecifier(WarningPingSound),
                    uid,
                    AudioParams.Default.WithMaxDistance(PingMaxDistance));
            }

            // Resolve the target from whichever combat component is active.
            EntityUid target = default;
            if (TryComp<NPCMeleeCombatComponent>(uid, out var melee))
                target = melee.Target;
            else if (TryComp<NPCRangedCombatComponent>(uid, out var ranged))
                target = ranged.Target;

            // If combat was already cleaned up, clear the warning.
            if (target == default)
            {
                RemCompDeferred<AggroWarningComponent>(uid);
                continue;
            }

            // Distance check: instant aggro if very close, de-aggro if out of vision range.
            if (TryComp<TransformComponent>(target, out var targetXform) &&
                xform.Coordinates.TryDistance(EntityManager, _transform, targetXform.Coordinates, out var distance))
            {
                // Within InstantAggroRange → skip the delay, attack immediately.
                if (distance <= warning.InstantAggroRange)
                {
                    RemCompDeferred<AggroWarningComponent>(uid);
                    continue;
                }

                // Target retreated beyond vision radius during the warning window → de-aggro.
                var visionRadius = 10f; // default; matches NPCBlackboard VisionRadius

                if (TryComp<HTNComponent>(uid, out var htn))
                {
                    visionRadius = htn.Blackboard.GetValueOrDefault<float>("VisionRadius", EntityManager);
                    if (visionRadius <= 0f)
                        visionRadius = 10f;
                }

                if (distance > visionRadius)
                {
                    // De-aggro: strip combat components so the NPC returns to idle.
                    RemCompDeferred<NPCMeleeCombatComponent>(uid);
                    RemCompDeferred<NPCRangedCombatComponent>(uid);
                    RemCompDeferred<AggroWarningComponent>(uid);
                    continue;
                }
            }

            // Count down the warning timer.
            warning.TimeRemaining -= frameTime;
            if (warning.TimeRemaining <= 0f)
            {
                // Timer expired — commit to attack (remove the warning gate).
                RemCompDeferred<AggroWarningComponent>(uid);
            }
        }
    }
}
