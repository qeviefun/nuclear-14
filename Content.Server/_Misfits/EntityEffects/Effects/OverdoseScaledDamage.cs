// #Misfits Change /Add:/ Overdose toxic scaling — deals Poison damage proportional to reagent excess above threshold
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.EntityEffects.Effects;

/// <summary>
///     Reagent effect that deals Poison damage scaled to how much reagent is
///     present in the solution above a configured threshold.
///     Represents the toxic load of unmetabolized drug excess — the more a player
///     stacks a chem past the safe limit, the more poisoning they suffer per tick.
///     Fires every metabolism tick (server-side, ~1/s) regardless of metabolismRate.
/// </summary>
[UsedImplicitly]
public sealed partial class OverdoseScaledDamage : EntityEffect
{
    /// <summary>
    ///     Reagent quantity above which toxic damage begins.
    ///     Should match or be near the threshold used on companion
    ///     <c>!type:HealthChange</c> / <c>!type:ReagentThreshold</c> blocks.
    /// </summary>
    [DataField(required: true)]
    public float Threshold;

    /// <summary>
    ///     Poison damage dealt per unit of reagent above the threshold, per tick.
    ///     e.g. 0.05 with 20u excess = 1.0 Poison per tick (~1 Poison/second).
    /// </summary>
    [DataField(required: true)]
    public float DamagePerUnit;

    public override void Effect(EntityEffectBaseArgs args)
    {
        // Only operate when metabolized inside a reagent solution context
        if (args is not EntityEffectReagentArgs reagentArgs
            || reagentArgs.Source == null
            || reagentArgs.Reagent == null)
            return;

        // Read the current reagent quantity from the solution
        var reagentId = new ReagentId(reagentArgs.Reagent.ID, null);
        if (!reagentArgs.Source.TryGetReagent(reagentId, out var reagentQty))
            return;

        var excess = (float) reagentQty.Quantity - Threshold;
        if (excess <= 0f)
            return;

        // Scale Poison damage linearly with how far above the threshold we are
        var damageAmount = FixedPoint2.New(excess * DamagePerUnit);
        if (damageAmount <= FixedPoint2.Zero)
            return;

        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict["Poison"] = damageAmount;

        var damageableSys = args.EntityManager.EntitySysManager.GetEntitySystem<DamageableSystem>();
        damageableSys.TryChangeDamage(args.TargetEntity, damageSpec, interruptsDoAfters: false);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-overdose-toxic", ("chance", Probability));
    }
}
