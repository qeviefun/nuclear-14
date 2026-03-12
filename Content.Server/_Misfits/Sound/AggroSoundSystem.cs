// Misfits Change - System to play aggro/alert sounds on combat entry, separate from idle ambient sounds
using Content.Shared._Misfits.Sound;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.NPC.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server._Misfits.Sound;

/// <summary>
/// Plays an aggro/alert sound the first time an entity with
/// <see cref="AggroSoundComponent"/> attacks (melee or ranged), with a cooldown
/// to prevent spam. Keeps combat vocalizations separate from idle ambient sounds.
/// </summary>
public sealed class AggroSoundSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AggroSoundComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<AggroSoundComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentInit>(OnMeleeCombatStartup);
        SubscribeLocalEvent<NPCRangedCombatComponent, ComponentInit>(OnRangedCombatStartup);
    }

    private void OnMeleeAttack(Entity<AggroSoundComponent> entity, ref MeleeAttackEvent args)
    {
        TryPlayAggro(entity);
    }

    private void OnGunShot(Entity<AggroSoundComponent> entity, ref GunShotEvent args)
    {
        // GunShotEvent fires on the gun entity. For mobs that ARE their own gun
        // (Gun component directly on the mob), this fires on the mob itself.
        TryPlayAggro(entity);
    }

    // Misfits Change /Fix: Prime the aggro icon as soon as hostile NPC combat starts,
    // so ranged mobs like assaultrons show the exclamation mark on aggro instead of only after their first attack.
    private void OnMeleeCombatStartup(EntityUid uid, NPCMeleeCombatComponent component, ComponentInit args)
    {
        if (TryComp<AggroSoundComponent>(uid, out var aggro))
            TryPlayAggro((uid, aggro));
    }

    private void OnRangedCombatStartup(EntityUid uid, NPCRangedCombatComponent component, ComponentInit args)
    {
        if (TryComp<AggroSoundComponent>(uid, out var aggro))
            TryPlayAggro((uid, aggro));
    }

    private void TryPlayAggro(Entity<AggroSoundComponent> entity)
    {
        if (entity.Comp.CooldownRemaining > 0f)
            return;

        _audio.PlayPvs(entity.Comp.Sound, entity.Owner);
        // Pick a random cooldown each play so mobs in a group do not vocalize in sync.
        entity.Comp.CooldownRemaining = _random.NextFloat(entity.Comp.CooldownMin, entity.Comp.CooldownMax);
        // Misfits Change /Fix: Dirty the component so clients see the updated CooldownRemaining
        // and the aggro status icon (ShowAggroIconSystem) appears correctly.
        Dirty(entity.Owner, entity.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AggroSoundComponent>();
        while (query.MoveNext(out var uid, out var aggro))
        {
            if (aggro.CooldownRemaining <= 0f)
                continue;

            aggro.CooldownRemaining -= frameTime;

            // Misfits Change /Fix: Dirty when cooldown expires so clients hide the aggro icon.
            if (aggro.CooldownRemaining <= 0f)
                Dirty(uid, aggro);
        }
    }
}
