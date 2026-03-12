// #Misfits Add - RMC holster visualizer ported from RMC-14 (MIT)
using Content.Shared._RMC.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client._RMC.Inventory;

public sealed class CMHolsterVisualizerSystem : VisualizerSystem<CMHolsterComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        CMHolsterComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite ||
            !sprite.LayerMapTryGet(CMHolsterLayers.Fill, out var layer))
            return;

        if (component.Contents.Count != 0)
        {
            sprite.LayerSetVisible(layer, true);
            return;
        }

        sprite.LayerSetVisible(layer, false);
    }
}
