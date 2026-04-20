using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Misfits.C27;

// #Misfits Add - Shared system that gates equipping of C-27 armor / helmet items: only entities
// whose HumanoidAppearanceComponent.Species == "C27" can put them on. Mirrors the pattern used
// by PowerArmorProficiencySystem so prediction cancels the equip animation immediately on the
// client side.
public sealed class MisfitsC27ArmorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MisfitsC27ArmorComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(Entity<MisfitsC27ArmorComponent> item, ref BeingEquippedAttemptEvent args)
    {
        // Trophy / admin-spawned variants: skip the gate entirely.
        if (!item.Comp.RequiresC27Species)
            return;

        // Non-humanoids (e.g. animal spawns, dummies) trivially can't wear C-27 armor.
        if (!TryComp<HumanoidAppearanceComponent>(args.EquipTarget, out var humanoid))
        {
            args.Reason = "c27-armor-species-required";
            args.Cancel();
            return;
        }

        // Species ProtoId compares case-sensitively as a string.
        if (humanoid.Species == "C27")
            return;

        args.Reason = "c27-armor-species-required";
        args.Cancel();
    }
}
