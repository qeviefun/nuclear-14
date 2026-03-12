// #Misfits Add - RMC clothing system ported from RMC-14 (MIT)
// Adapted: removed SharedUniformAccessorySystem dependency
using System.Linq;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC.Clothing;

public sealed class RMCClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<ClothingLimitComponent> _clothingLimitQuery;

    public override void Initialize()
    {
        _clothingLimitQuery = GetEntityQuery<ClothingLimitComponent>();

        SubscribeLocalEvent<ClothingLimitComponent, BeingEquippedAttemptEvent>(OnClothingLimitBeingEquippedAttempt);
        SubscribeLocalEvent<ClothingRequireEquippedComponent, BeingEquippedAttemptEvent>(OnRequireEquippedBeingEquippedAttempt);
        SubscribeLocalEvent<ClothingComponent, DroppedEvent>(OnDropped);
    }

    private void OnClothingLimitBeingEquippedAttempt(Entity<ClothingLimitComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var slots = _inventory.GetSlotEnumerator(args.EquipTarget, ent.Comp.Slot);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } contained)
                continue;

            if (!_clothingLimitQuery.TryComp(contained, out var limit))
                continue;

            if (limit.Id == ent.Comp.Id && contained != ent.Owner)
            {
                args.Cancel();
                args.Reason = Loc.GetString("rmc-clothing-limit-already-equipped");
                return;
            }
        }
    }

    private void OnRequireEquippedBeingEquippedAttempt(Entity<ClothingRequireEquippedComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Whitelist is not { } whitelist)
            return;

        if (!HasEquippedItemsWithinWhitelist(args.EquipTarget, whitelist))
        {
            args.Cancel();
            args.Reason = Loc.GetString(ent.Comp.DenyReason);
        }
    }

    private void OnDropped(Entity<ClothingComponent> ent, ref DroppedEvent args)
    {
        // Auto-unequip items that require this clothing
        var slots = _inventory.GetSlotEnumerator(args.User);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } contained)
                continue;

            if (!TryComp(contained, out ClothingRequireEquippedComponent? require))
                continue;

            if (!require.AutoUnequip)
                continue;

            if (require.Whitelist is not { } whitelist)
                continue;

            if (HasEquippedItemsWithinWhitelist(args.User, whitelist))
                continue;

            _inventory.TryUnequip(args.User, slot.ID, force: true);
            _hands.TryPickupAnyHand(args.User, contained);
        }
    }

    private bool HasEquippedItemsWithinWhitelist(EntityUid user, EntityWhitelist whitelist)
    {
        // Check hands
        if (_hands.EnumerateHeld(user).Any(held => _whitelist.IsWhitelistPass(whitelist, held)))
            return true;

        // Check inventory
        var slots = _inventory.GetSlotEnumerator(user);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } contained &&
                _whitelist.IsWhitelistPass(whitelist, contained))
            {
                return true;
            }
        }

        return false;
    }
}
