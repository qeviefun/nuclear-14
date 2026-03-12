// #Misfits Change - Reworked to use IGameTiming-based deterministic cycle (no per-frame dirty spam)
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC14.DayNightCycle
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class DayNightCycleComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cycleDuration")]
        public float CycleDurationMinutes { get; set; } = 90f; // Default cycle duration is 90 minutes

        /// <summary>
        /// Offset into the cycle (0–1) applied at startup so the world begins at
        /// "early morning" rather than midnight.
        /// </summary>
        [DataField("startOffset")]
        [AutoNetworkedField]
        public float StartOffset { get; set; } = 0.2f; // Start at 20% (early morning)

        [DataField("timeEntries")]
        public List<TimeEntry> TimeEntries { get; set; } = new()
        {
            new() { Time = 0.00f, ColorHex = "#26262C" }, // Midnight       – lifted grey-black so silhouettes remain readable
            new() { Time = 0.03f, ColorHex = "#2B2B32" }, // Deep night     – still dim, but not crushed to black
            new() { Time = 0.06f, ColorHex = "#33333D" }, // Late night     – subtle cool grey before dawn warmth starts
            new() { Time = 0.10f, ColorHex = "#463C33" }, // Pre-dawn       – faint brown-grey on the horizon
            new() { Time = 0.14f, ColorHex = "#66513D" }, // First light    – muted desert bronze
            new() { Time = 0.17f, ColorHex = "#85684A" }, // Dawn           – compact sunrise ramp, about 15 minutes in
            new() { Time = 0.25f, ColorHex = "#A27C55" }, // Early morning  – warm tan, more gradual lift into day
            new() { Time = 0.35f, ColorHex = "#BB9764" }, // Morning        – steady brightening with less abrupt jump
            new() { Time = 0.45f, ColorHex = "#D4B27A" }, // Late morning   – smoother approach toward noon brightness
            new() { Time = 0.55f, ColorHex = "#E2C692" }, // Noon           – bright, dusty sun rather than harsh white-gold
            new() { Time = 0.65f, ColorHex = "#CDA66F" }, // Early afternoon – begins easing down from noon more gently
            new() { Time = 0.75f, ColorHex = "#A9794B" }, // Afternoon      – warm ochre descent toward dusk
            new() { Time = 0.83f, ColorHex = "#745339" }, // Dusk           – compact sunset ramp, about 15 minutes before midnight
            new() { Time = 0.90f, ColorHex = "#4A3E3A" }, // Early night    – warm grey-brown rather than near-black
            new() { Time = 0.97f, ColorHex = "#313138" }, // Late night     – returns to cool grey before midnight
            new() { Time = 1.00f, ColorHex = "#26262C" }  // Back to Midnight
        };
    }

    [DataDefinition, NetSerializable, Serializable]
    public sealed partial class TimeEntry
    {
        [DataField("colorHex")]
        public string ColorHex { get; set; } = "#FFFFFF";

        [DataField("time")]
        public float Time { get; set; } // Normalized time (0-1)
    }
}