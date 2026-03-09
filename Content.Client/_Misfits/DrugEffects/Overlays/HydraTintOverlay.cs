// #Misfits Change /Add:/ Hydra red tint overlay
using System.Numerics;
using Content.Client._Misfits.DrugEffects;
using Content.Shared._Misfits.DrugEffects;
using Content.Shared.StatusEffect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Misfits.DrugEffects.Overlays;

/// <summary>
///     Applies a hot red tint while hydra is active.
/// </summary>
public sealed class HydraTintOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    public float CurrentPower;
    private float _visualScale;

    private static readonly Vector3 TintColor = new(0.62f, 0.11f, 0.09f);
    private const float PowerDivisor = 24f;
    private const float MaxTintAmount = 0.32f;

    public HydraTintOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("ColorTint").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;
        if (playerEntity == null)
            return;

        if (!_entityManager.HasComponent<HydraTintComponent>(playerEntity)
            || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
            return;

        var statusEffects = _systems.GetEntitySystem<StatusEffectsSystem>();
        if (!statusEffects.TryGetTime(playerEntity.Value, MisfitsDrugOverlaySystem.HydraTintKey, out var time, status))
            return;

        var timeLeft = (float) (time.Value.Item2 - _timing.CurTime).TotalSeconds;
        CurrentPower += 8f * (0.5f * timeLeft - CurrentPower) * args.DeltaSeconds / (timeLeft + 1f);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eye))
            return false;

        if (args.Viewport.Eye != eye.Eye)
            return false;

        _visualScale = Math.Clamp(CurrentPower / PowerDivisor, 0f, 1f);
        return _visualScale > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint_color", TintColor);
        _shader.SetParameter("tint_amount", _visualScale * MaxTintAmount);

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}