using Content.Shared._NC.RandomAccessKey;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("accessLevel")]
    public sealed partial class AccessLevelPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; set; } = default!; // Forge-Change

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        [DataField("name")]
        public string? Name { get; set; }

        public string GetAccessLevelName()
        {
            // #Misfits Change /Fix/: malformed whitespace access-level names should fall back to the raw ID
            // instead of triggering localization warnings when a reader/UI asks for a display name.
            if (!string.IsNullOrWhiteSpace(Name) && Name is { } name)
                return Loc.GetString(name);

            return ID;
        }
    }
}
