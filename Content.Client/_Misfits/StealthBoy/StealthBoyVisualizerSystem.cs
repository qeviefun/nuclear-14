// #Misfits Change - Stealth Boy visuals now use the shared stealth system so the cloak
// affects the full humanoid sprite consistently. This system remains as a no-op stub to
// preserve the existing registration point without reintroducing manual alpha changes.

namespace Content.Client._Misfits.StealthBoy;

/// <summary>
/// Stealth Boy visuals are handled by the core stealth shader system.
/// </summary>
public sealed class StealthBoyVisualizerSystem : EntitySystem
{
}
