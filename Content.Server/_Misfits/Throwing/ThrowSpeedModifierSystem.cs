// #Misfits Change - Applies per-item throw speed tuning for wasteland throwables
using Content.Shared._Misfits.Throwing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Throwing;

namespace Content.Server._Misfits.Throwing;

/// <summary>
/// Adjusts throw speed based on the thrown item's explicit modifier component.
/// </summary>
public sealed class ThrowSpeedModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, BeforeThrowEvent>(OnBeforeThrow);
    }

    private void OnBeforeThrow(Entity<HandsComponent> ent, ref BeforeThrowEvent args)
    {
        if (args.Cancelled ||
            !TryComp<ThrowSpeedModifierComponent>(args.ItemUid, out var modifier) ||
            modifier.Multiplier <= 0f ||
            modifier.Multiplier == 1f)
        {
            return;
        }

        args.ThrowSpeed *= modifier.Multiplier;
    }
}