// #Misfits Add: Component that grants power armor a hotkey-activated brace stance.
// When IsBraced, the wearer is anchored (immobile) but can still fire weapons.
// A damage modifier set applies while braced, providing slight extra resistance.
// The toggle action carries a 5-second cooldown (useDelay in the action prototype)
// so the wearer cannot rapidly switch between braced and mobile states.

using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.PowerArmor;

/// <summary>
///     Add to a power armor item entity to give the wearer a toggle hotkey action that
///     anchors them in place (immobile but still able to shoot) and applies extra damage
///     reduction while the stance is active. A 5-second cooldown defined on the action
///     prototype prevents rapid cycling between states.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PowerArmorBraceSystem))]
public sealed partial class PowerArmorBraceComponent : Component
{
    /// <summary>Whether the wearer is currently braced/anchored.</summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsBraced;

    /// <summary>The entity currently wearing and controlling this armor.</summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Wearer;

    /// <summary>ProtoId of the toggle action entity granted to the wearer.</summary>
    [DataField]
    public EntProtoId BraceAction = "ActionTogglePowerArmorBrace";

    /// <summary>Spawned action entity stored on the armor item itself.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BraceActionEntity;

    /// <summary>
    ///     Inventory slot flag the armor must occupy for the action to be granted.
    ///     Defaults to OUTERCLOTHING (standard power armor slot).
    /// </summary>
    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredSlot = SlotFlags.OUTERCLOTHING;

    /// <summary>
    ///     Extra damage modifier set applied on top of the suit's ArmorComponent modifiers
    ///     while <see cref="IsBraced"/> is <c>true</c>.
    ///     Configure per-suit in YAML — empty by default (no bonus).
    /// </summary>
    [DataField]
    public DamageModifierSet ActiveModifiers = new();
}
