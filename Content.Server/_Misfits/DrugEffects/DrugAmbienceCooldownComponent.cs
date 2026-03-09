// #Misfits Change /Add:/ Drug ambience cooldown tracking
namespace Content.Server._Misfits.DrugEffects;

/// <summary>
///     Tracks per-entity cooldowns for passive drug ambience messages.
/// </summary>
[RegisterComponent]
public sealed partial class DrugAmbienceCooldownComponent : Component
{
    public Dictionary<string, TimeSpan> NextAllowedById = new();
}