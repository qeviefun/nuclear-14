// #Misfits Add - RMC fixed item size storage component ported from RMC-14 (MIT)
// Makes all items in this storage use a fixed shape size, ignoring their real shape.
// This allows slot-based storage (e.g. magazine belts) where each item occupies one "slot".
using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedItemSystem))]
public sealed partial class FixedItemSizeStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2i Size = new(2, 2);

    public Box2i[]? CachedSize;
}
