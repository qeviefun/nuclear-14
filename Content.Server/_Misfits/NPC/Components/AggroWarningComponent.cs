// #Misfits Add - Aggro warning system: mobs ping and delay 2 seconds before
// committing to attack. If the player retreats during the delay the mob de-aggros.
// Instant aggro override when target enters within InstantAggroRange tiles.
namespace Content.Server._Misfits.NPC.Components;

/// <summary>
/// Added to an NPC when it first detects a hostile target. While this component
/// exists the NPC will not pursue or attack, giving the player time to back off.
/// </summary>
[RegisterComponent]
public sealed partial class AggroWarningComponent : Component
{
    /// <summary>
    /// Seconds remaining in the warning window.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeRemaining = 2f;

    /// <summary>
    /// If the target is within this many tiles, skip the delay and attack immediately.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float InstantAggroRange = 5f;

    /// <summary>
    /// Whether the warning ping has already been played for this aggro window.
    /// </summary>
    [ViewVariables]
    public bool PingPlayed;
}
