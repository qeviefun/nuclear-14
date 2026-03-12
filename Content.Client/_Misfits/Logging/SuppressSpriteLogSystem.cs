// #Misfits Fix - Suppresses console spam from missing _RMC RSI texture files.
// The RMC content references many RSI paths that are not present in this fork,
// causing a flood of [ERRO] go.comp.sprite messages on every map load.
// Setting the sawmill level to Fatal hides those Error-level messages without
// losing anything actionable; genuine fatal sprite failures will still surface.
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client._Misfits.Logging;

/// <summary>
/// Raises the <c>go.comp.sprite</c> sawmill level to <see cref="LogLevel.Fatal"/>
/// so that missing-RSI error spam from unported _RMC textures is hidden from the console.
/// </summary>
public sealed class SuppressSpriteLogSystem : EntitySystem
{
    [Dependency] private readonly ILogManager _logManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Misfits Fix: silence the flood of "Unable to load RSI" errors that come
        // from _RMC content whose texture files don't exist in this fork.
        _logManager.GetSawmill("go.comp.sprite").Level = LogLevel.Fatal;
    }
}
