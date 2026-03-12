using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Power;
using Content.Shared.Temperature;
using Content.Shared.Interaction;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolutionHeaterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SolutionHeaterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<SolutionHeaterComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<SolutionHeaterComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<SolutionHeaterComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void TurnOn(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, true);
        EnsureComp<ActiveSolutionHeaterComponent>(uid);
    }

    public bool TryTurnOn(EntityUid uid, ItemPlacerComponent? placer = null)
    {
        if (!Resolve(uid, ref placer))
            return false;

        var heater = Comp<SolutionHeaterComponent>(uid);

        if (placer.PlacedEntities.Count <= 0 || !_powerReceiver.IsPowered(uid))
            return false;

        if (heater.RequireHot)
        {
            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(uid, isHotEvent);
            if (!isHotEvent.IsHot)
                return false;
        }

        TurnOn(uid);
        return true;
    }

    public void TurnOff(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, false);
        RemComp<ActiveSolutionHeaterComponent>(uid);
    }

    private void OnPowerChanged(Entity<SolutionHeaterComponent> entity, ref PowerChangedEvent args)
    {
        var placer = Comp<ItemPlacerComponent>(entity);
        if (args.Powered && TryTurnOn(entity, placer))
        {
            return;
        }

        TurnOff(entity);
    }

    private void OnRefreshParts(Entity<SolutionHeaterComponent> entity, ref RefreshPartsEvent args)
    {
        var heatRating = args.PartRatings[entity.Comp.MachinePartHeatMultiplier] - 1;

        entity.Comp.HeatPerSecond = entity.Comp.BaseHeatPerSecond * MathF.Pow(entity.Comp.PartRatingHeatMultiplier, heatRating);
    }

    private void OnUpgradeExamine(Entity<SolutionHeaterComponent> entity, ref UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("solution-heater-upgrade-heat", entity.Comp.HeatPerSecond / entity.Comp.BaseHeatPerSecond);
    }

    private void OnItemPlaced(Entity<SolutionHeaterComponent> entity, ref ItemPlacedEvent args)
    {
        if (!TryTurnOn(entity))
            TurnOff(entity);
    }

    private void OnItemRemoved(Entity<SolutionHeaterComponent> entity, ref ItemRemovedEvent args)
    {
        var placer = Comp<ItemPlacerComponent>(entity);
        if (placer.PlacedEntities.Count == 0) // Last entity was removed
            TurnOff(entity);
    }

    // #Misfits Change /Fix/ Let single-item heater surfaces like the hotplate hand back their contents on activate.
    private void OnActivateInWorld(Entity<SolutionHeaterComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp<ItemPlacerComponent>(entity, out var placer) || placer.PlacedEntities.Count != 1)
            return;

        foreach (var placedEntity in placer.PlacedEntities)
        {
            if (_hands.TryPickupAnyHand(args.User, placedEntity))
                args.Handled = true;

            break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out _, out _, out var heater, out var placer))
        {
            foreach (var heatingEntity in placer.PlacedEntities)
            {
                if (!TryComp<SolutionContainerManagerComponent>(heatingEntity, out var container))
                    continue;

                var energy = heater.HeatPerSecond * frameTime;
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((heatingEntity, container)))
                {
                    _solutionContainer.AddThermalEnergy(soln, energy);
                }
            }
        }
    }
}
