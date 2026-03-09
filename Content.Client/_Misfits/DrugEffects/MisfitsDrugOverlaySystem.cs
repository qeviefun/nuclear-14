// #Misfits Change /Add:/ Client drug ambience overlays
using Content.Client._Misfits.DrugEffects.Overlays;
using Content.Shared._Misfits.DrugEffects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Misfits.DrugEffects;

/// <summary>
///     Handles local overlays for custom Nuclear14 drug visual effects.
/// </summary>
public sealed class MisfitsDrugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public const string HealingPowderHazeKey = "HealingPowderHaze";
    public const string HydraTintKey = "HydraTint";

    private HealingPowderHazeOverlay _healingPowderHaze = default!;
    private HydraTintOverlay _hydraTint = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealingPowderHazeComponent, ComponentInit>(OnHealingPowderInit);
        SubscribeLocalEvent<HealingPowderHazeComponent, ComponentShutdown>(OnHealingPowderShutdown);
        SubscribeLocalEvent<HealingPowderHazeComponent, LocalPlayerAttachedEvent>(OnHealingPowderAttached);
        SubscribeLocalEvent<HealingPowderHazeComponent, LocalPlayerDetachedEvent>(OnHealingPowderDetached);

        SubscribeLocalEvent<HydraTintComponent, ComponentInit>(OnHydraInit);
        SubscribeLocalEvent<HydraTintComponent, ComponentShutdown>(OnHydraShutdown);
        SubscribeLocalEvent<HydraTintComponent, LocalPlayerAttachedEvent>(OnHydraAttached);
        SubscribeLocalEvent<HydraTintComponent, LocalPlayerDetachedEvent>(OnHydraDetached);

        _healingPowderHaze = new HealingPowderHazeOverlay();
        _hydraTint = new HydraTintOverlay();
    }

    private void OnHealingPowderAttached(EntityUid uid, HealingPowderHazeComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_healingPowderHaze);
    }

    private void OnHealingPowderDetached(EntityUid uid, HealingPowderHazeComponent component, LocalPlayerDetachedEvent args)
    {
        _healingPowderHaze.CurrentPower = 0f;
        _overlayManager.RemoveOverlay(_healingPowderHaze);
    }

    private void OnHealingPowderInit(EntityUid uid, HealingPowderHazeComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayManager.AddOverlay(_healingPowderHaze);
    }

    private void OnHealingPowderShutdown(EntityUid uid, HealingPowderHazeComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity != uid)
            return;

        _healingPowderHaze.CurrentPower = 0f;
        _overlayManager.RemoveOverlay(_healingPowderHaze);
    }

    private void OnHydraAttached(EntityUid uid, HydraTintComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_hydraTint);
    }

    private void OnHydraDetached(EntityUid uid, HydraTintComponent component, LocalPlayerDetachedEvent args)
    {
        _hydraTint.CurrentPower = 0f;
        _overlayManager.RemoveOverlay(_hydraTint);
    }

    private void OnHydraInit(EntityUid uid, HydraTintComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayManager.AddOverlay(_hydraTint);
    }

    private void OnHydraShutdown(EntityUid uid, HydraTintComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity != uid)
            return;

        _hydraTint.CurrentPower = 0f;
        _overlayManager.RemoveOverlay(_hydraTint);
    }
}