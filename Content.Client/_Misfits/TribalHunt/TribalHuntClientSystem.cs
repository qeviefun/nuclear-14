using Content.Client._Misfits.TribalHunt.UI;
using Content.Shared._Misfits.TribalHunt;

namespace Content.Client._Misfits.TribalHunt;

/// <summary>
/// Receives tribal hunt UI state updates and keeps the hunt window synchronized.
/// </summary>
public sealed class TribalHuntClientSystem : EntitySystem
{
    private TribalHuntWindow? _window;
    private TribalHuntUiState _latestState = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TribalHuntUiUpdateEvent>(OnUiUpdate);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _window?.Close();
        _window = null;
    }

    private void OnUiUpdate(TribalHuntUiUpdateEvent msg)
    {
        _latestState = msg.State;
        EnsureWindow();
        _window!.UpdateState(_latestState);

        if (!_window.IsOpen)
            _window.OpenCentered();
    }

    private void EnsureWindow()
    {
        if (_window != null)
            return;

        _window = new TribalHuntWindow();
        _window.OnClose += () => _window = null;
    }
}