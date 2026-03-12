// #Misfits Add - RMC limited storage system ported from RMC-14 (MIT)
// Prevents inserting items that exceed per-category limits (e.g. max 1 gun in holster belt)
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._RMC.Storage;

public sealed class RMCStorageLimitSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<StorageComponent> _storageQuery;

    public override void Initialize()
    {
        _storageQuery = GetEntityQuery<StorageComponent>();

        SubscribeLocalEvent<LimitedStorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(EntityUid uid, LimitedStorageComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != StorageComponent.ContainerId)
            return;

        if (!CanInsertStorageLimit((uid, comp), args.EntityUid, out var popup))
        {
            args.Cancel();

            // Try to show a popup to the user interacting
            if (popup != default)
                _popup.PopupEntity(Loc.GetString(popup), uid, PopupType.SmallCaution);
        }
    }

    private bool CanInsertStorageLimit(Entity<LimitedStorageComponent> limited, EntityUid toInsert, out LocId popup)
    {
        popup = default;
        if (!_storageQuery.TryComp(limited, out var storage))
            return true;

        foreach (var limit in limited.Comp.Limits)
        {
            if (!_entityWhitelist.IsWhitelistPassOrNull(limit.Whitelist, toInsert))
                continue;

            if (_entityWhitelist.IsBlacklistPass(limit.Blacklist, toInsert))
                continue;

            var storedCount = 0;
            foreach (var stored in storage.StoredItems.Keys)
            {
                if (stored == toInsert)
                    continue;

                if (!_entityWhitelist.IsWhitelistPassOrNull(limit.Whitelist, stored))
                    continue;

                if (_entityWhitelist.IsBlacklistPass(limit.Blacklist, stored))
                    continue;

                storedCount++;
                if (storedCount >= limit.Count)
                    break;
            }

            if (storedCount < limit.Count)
                continue;

            popup = limit.Popup == default ? "rmc-storage-limit-cant-fit" : limit.Popup;
            return false;
        }

        return true;
    }
}
