// #Misfits Change /Fix/ Gate bonfire heating and ambience behind the live fire state.
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Audio;
using Content.Shared.Placeable;
using Content.Shared.Temperature;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles <see cref="BonfireHeaterComponent"/> updating and events.
/// </summary>
public sealed class BonfireHeaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SolutionHeaterSystem _solutionHeater = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<BonfireHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var comp, out var placer))
        {
            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(uid, isHotEvent);

            _ambientSound.SetAmbience(uid, isHotEvent.IsHot);

            if (TryComp<SolutionHeaterComponent>(uid, out _))
            {
                if (isHotEvent.IsHot)
                    _solutionHeater.TryTurnOn(uid, placer);
                else
                    _solutionHeater.TurnOff(uid);
            }

            if (!isHotEvent.IsHot)
                continue;

            var heatToAdd = comp.BaseHeatMultiplier;
            foreach (var ent in placer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, heatToAdd, true);
            }
        }
    }
}
