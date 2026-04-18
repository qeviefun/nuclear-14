using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Construction;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._NC.RandomAccessKey;

public sealed class RandomAccessKeySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!; // #Misfits Fix - allow toggling door when key is used on it
    private const string RandomAccessPrefix = "RandomAccess";
    private const string Key = "N14IDKeyIronEmpty";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAccessKeyComponent, ConstructionCompletedEvent>(OmConstructionCompleted);
        // #Misfits Fix - clicking the locked door with the key in hand had no effect because doors only react
        // to empty-hand ActivateInWorldEvent. Forward InteractUsingEvent into the standard door toggle path so
        // the existing AccessReader check (which already enumerates the user's hands) accepts the held key.
        SubscribeLocalEvent<RandomAccessKeyComponent, InteractUsingEvent>(OnKeyInteractUsing);
    }

    private void OmConstructionCompleted(Entity<RandomAccessKeyComponent> ent,
        ref ConstructionCompletedEvent args)
    {
        if (args.UserUid == null)
            return;

        if (!TryComp(ent.Owner, out DoorComponent? door))
            return;

        var randomKey = _random.Next(1000, 9999);
        var prototypeId = $"{RandomAccessPrefix}{randomKey}";
        // #Misfits Removed - dead AccessLevelPrototype object: it was never registered with IPrototypeManager
        // and only its .ID (a plain string) was consumed below. The string id is sufficient by itself.
        // var prototype = new AccessLevelPrototype
        // {
        //     ID = prototypeId,
        //     Name = $"Key #{randomKey}"
        // };
        var accessReader = EnsureComp<AccessReaderComponent>(ent.Owner);

        accessReader.AccessLists.Add(new HashSet<ProtoId<AccessLevelPrototype>> { prototypeId });
        // #Misfits Fix - dirty the access reader so the client sees the new access requirement; without this
        // the client predicted state of the door has an empty AccessLists and prediction diverges from server.
        Dirty(ent.Owner, accessReader);
        // #Misfits Fix - notify NavMap/UI consumers (e.g., door electronics UI) that the access list changed.
        RaiseLocalEvent(ent.Owner, new AccessReaderConfigurationChangedEvent());

        var userCord = _transform.GetMapCoordinates(args.UserUid.Value);
        var doorKey = Spawn(Key, userCord);
        var accessKey = EnsureComp<AccessComponent>(doorKey);

        _meta.SetEntityName(doorKey, $"Key #{randomKey}");
        accessKey.Tags.Clear();
        accessKey.Tags.Add(prototypeId);
        Dirty(doorKey, accessKey);
        _hands.PickupOrDrop(args.UserUid.Value, doorKey); // #Misfits Fix - method expects EntityUid not EntityUid?
        door.CanPry = false;
        door.BumpOpen = false;
        Dirty(ent.Owner, door);
    }

    // #Misfits Add - bridge InteractUsing -> door toggle so the spawned key (or any matching ID) actually
    // opens/closes the locked door when used on it. The standard door OnActivate path only handles empty-hand
    // clicks, so without this the player had no way to use the key after picking it up.
    private void OnKeyInteractUsing(Entity<RandomAccessKeyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Only react when the held item actually carries access tags, otherwise let other handlers run
        // (e.g., construction tools deconstructing the door, prying, welding, etc.).
        if (!HasComp<AccessComponent>(args.Used))
            return;

        if (!TryComp(ent.Owner, out DoorComponent? door))
            return;

        if (_door.TryToggleDoor(ent.Owner, door, args.User, predicted: true))
            args.Handled = true;
    }
}
