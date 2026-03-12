using Content.Server._Misfits.CombatMode;
using Content.Server.NPC.HTN;
using Content.Shared.CombatMode;

namespace Content.Server.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
{
    protected override bool IsNpc(EntityUid uid)
    {
        return HasComp<HTNComponent>(uid);
    }

    // Misfits Add - Override to raise CombatModeActivatedEvent when combat mode turns ON.
    // This allows CombatModePingSystem to react without subscribing to the exclusive
    // (CombatModeComponent, ToggleCombatActionEvent) slot already owned by SharedCombatModeSystem.
    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return;

        var wasActive = component.IsInCombatMode;
        base.SetInCombatMode(entity, value, component);

        if (value && !wasActive)
            RaiseLocalEvent(entity, new CombatModeActivatedEvent());
    }
}
