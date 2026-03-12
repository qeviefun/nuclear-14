// #Misfits Add - Marker component for entities spawned through the Persistent Entity Spawn Menu.
// Stores the unique persistence ID used to match entries in persistent_entities.json.
using Robust.Shared.GameObjects;

namespace Content.Shared._Misfits.PersistentSpawn;

/// <summary>
/// Attached to every entity spawned via the Persistent Entity Spawn Menu.
/// Contains the unique persistence key so the server system can remove
/// the JSON entry when the entity is erased through either spawn panel.
/// </summary>
[RegisterComponent]
public sealed partial class MisfitsPersistentEntityComponent : Component
{
    /// <summary>Unique ID matching the JSON persistence store entry.</summary>
    [DataField]
    public string PersistenceId { get; set; } = string.Empty;
}
