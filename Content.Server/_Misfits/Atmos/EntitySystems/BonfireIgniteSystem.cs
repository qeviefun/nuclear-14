// #Misfits Change /Add/ Right-click "Light" verb for the bonfire so players can ignite it with a hot item in hand.
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._Misfits.Atmos.EntitySystems;

/// <summary>
/// Adds a right-click "Light" verb to the bonfire.
/// Ignites using whatever hot item (lighter, torch, etc.) the player holds in their active hand.
/// </summary>
public sealed class BonfireIgniteSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Gate on BonfireHeaterComponent so this verb only appears on actual bonfires
        SubscribeLocalEvent<BonfireHeaterComponent, GetVerbsEvent<ActivationVerb>>(OnGetLightVerb);
    }

    private void OnGetLightVerb(Entity<BonfireHeaterComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Only show when the bonfire is not already burning
        if (!TryComp<FlammableComponent>(ent, out var flammable) || flammable.OnFire)
            return;

        var user = args.User;
        var uid = ent.Owner;

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("bonfire-ignite-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => TryLightBonfire(uid, user, flammable),
        });
    }

    /// <summary>
    /// Checks if the user holds a hot item and, if so, ignites the bonfire.
    /// Shows a popup if no suitable ignition source is in hand.
    /// </summary>
    private void TryLightBonfire(EntityUid bonfire, EntityUid user, FlammableComponent flammable)
    {
        // Require something hot in the active hand
        if (!_hands.TryGetActiveItem(user, out var held))
        {
            _popup.PopupEntity(Loc.GetString("bonfire-ignite-no-source"), bonfire, user);
            return;
        }

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(held.Value, isHotEvent);

        if (!isHotEvent.IsHot)
        {
            _popup.PopupEntity(Loc.GetString("bonfire-ignite-no-source"), bonfire, user);
            return;
        }

        _flammable.Ignite(bonfire, held.Value, flammable, user);
    }
}
