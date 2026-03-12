// #Misfits Add - PipBoy Groups UIFragment wrapper for the CartridgeLoader.
using Content.Client.UserInterface.Fragments;
using Content.Shared._Misfits.PipBoy;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._Misfits.PipBoy;

public sealed partial class PipBoyGroupsUi : UIFragment
{
    private PipBoyGroupsUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new PipBoyGroupsUiFragment();

        _fragment.OnSendMessage += (type, targetNumber, groupId, content) =>
        {
            var msg = new PipBoyHubUiMessageEvent(type, targetNumber, groupId, content);
            var wrapper = new CartridgeUiMessage(msg);
            userInterface.SendMessage(wrapper);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is PipBoyHubUiState cast)
            _fragment?.UpdateState(cast);
    }
}
