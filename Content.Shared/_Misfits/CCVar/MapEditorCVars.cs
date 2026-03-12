// #Misfits Change /Add/ Offline map editor configuration.
// Shared placement keeps the cvar visible to the normal server/client builds the launcher now uses.
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Misfits.CCVar;

[CVarDefs]
public sealed class MapEditorCVars : CVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("mapeditor.enabled", false, CVar.SERVERONLY);
}