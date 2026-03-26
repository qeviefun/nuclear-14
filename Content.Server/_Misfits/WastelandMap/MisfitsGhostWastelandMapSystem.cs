// Grants the Wendover map action to ghosted observers so they can view the
// area map while spectating. The MobObserver entity already carries
// WastelandMapComponent + UserInterfaceComponent (added in observer.yml), so
// the OpenUiActionEvent fired by this action opens the map BUI directly on
// the ghost entity itself.
using Content.Shared.Actions;
using Content.Shared.Ghost;
using Robust.Shared.GameObjects;

namespace Content.Server._Misfits.WastelandMap;

public sealed class MisfitsGhostWastelandMapSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Subscribe to ghost MapInit so the action is available the moment a
        // player ghosts / becomes an observer.
        SubscribeLocalEvent<GhostComponent, MapInitEvent>(OnGhostMapInit);
    }

    private void OnGhostMapInit(EntityUid uid, GhostComponent component, MapInitEvent args)
    {
        // Use a local ref — the spawned action entity is owned by the ghost and
        // will be cleaned up automatically when the ghost entity is deleted.
        EntityUid? mapActionEntity = null;
        _actions.AddAction(uid, ref mapActionEntity, "ActionGhostViewWastelandMap");
    }
}
