// #Misfits Change /Add/ - Marks empty cardboard ammo boxes that can fold back into crafting material.
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Weapons.Ranged;

[RegisterComponent]
public sealed partial class FoldableAmmoBoxComponent : Component
{
    [DataField("refundMaterial")]
    public ProtoId<MaterialPrototype> RefundMaterial = "Cardboard";

    [DataField("refundAmount")]
    public int RefundAmount = 100;
}