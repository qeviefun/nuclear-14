// #Misfits Add - Stealth Boy item component. When activated, grants the holder
// temporary invisibility for Duration seconds without altering their inventory state.
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.StealthBoy;

/// <summary>
/// Placed on the Stealth Boy item. Activating it (Z-key / Use In Hand) applies
/// temporary stealth to the activating player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StealthBoyComponent : Component
{
    /// <summary>
    /// How long the invisibility lasts once activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Fade-in time — how long it takes to reach minimum opacity.
    /// </summary>
    [DataField]
    public TimeSpan FadeInTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Fade-out time — how long it takes to return to full opacity after deactivation.
    /// </summary>
    [DataField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Target stealth visibility while cloaked (-1 = fully hidden, 1 = fully visible).
    /// </summary>
    [DataField]
    public float Visibility = -1f;
}
