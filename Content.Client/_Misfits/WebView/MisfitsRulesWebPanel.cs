// #Misfits Add - WebView-backed rules popup; replaces legacy RulesControl/RulesPopup
using System;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Misfits.WebView;

/// <summary>
/// Full-screen rules overlay that displays the live wiki Rules page via CEF WebView.
/// Drop-in replacement for <c>RulesPopup</c> — exposes the same Timer/OnQuitPressed/OnAcceptPressed API.
/// </summary>
public sealed class MisfitsRulesWebPanel : Control
{
    private const string RulesUrl = "https://ss14.misfitsystems.net/wiki/index.php/Rules";

    private float _timer;
    private readonly Button _acceptButton;
    private readonly Label _waitLabel;

    public event Action? OnQuitPressed;
    public event Action? OnAcceptPressed;

    public float Timer
    {
        get => _timer;
        set
        {
            _timer = value;
            _waitLabel.Text = Loc.GetString("ui-rules-wait", ("time", (int) MathF.Floor(value)));
        }
    }

    public MisfitsRulesWebPanel()
    {
        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;
        VerticalExpand = true;

        // Outer container — centers panel on screen
        var outer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        };

        // Styled panel capped at a comfortable reading size
        var panel = new PanelContainer
        {
            MaxWidth = 900f,
            MaxHeight = 760f,
            HorizontalExpand = true,
            VerticalExpand = true
        };
        panel.AddStyleClass("windowPanel");

        var inner = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(10),
            HorizontalExpand = true,
            VerticalExpand = true
        };

        // Live wiki rules page via CEF
        var webView = new MisfitsWebViewControl
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MinHeight = 500f
        };
        webView.NavigateTo(RulesUrl);

        _waitLabel = new Label
        {
            Text = Loc.GetString("ui-rules-wait", ("time", 0))
        };

        _acceptButton = new Button
        {
            Text = Loc.GetString("ui-rules-accept"),
            Disabled = true,
            HorizontalExpand = true
        };
        var quitButton = new Button
        {
            Text = Loc.GetString("ui-escape-quit"),
            HorizontalExpand = true
        };
        quitButton.AddStyleClass("Caution");

        _acceptButton.OnPressed += _ => OnAcceptPressed?.Invoke();
        quitButton.OnPressed += _ => OnQuitPressed?.Invoke();

        var buttonRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true
        };
        buttonRow.AddChild(_acceptButton);
        buttonRow.AddChild(quitButton);

        inner.AddChild(webView);
        inner.AddChild(_waitLabel);
        inner.AddChild(buttonRow);
        panel.AddChild(inner);
        outer.AddChild(panel);
        AddChild(outer);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_acceptButton.Disabled)
            return;

        if (_timer > 0f)
        {
            _timer = Math.Max(0f, _timer - args.DeltaSeconds);
            _waitLabel.Text = Loc.GetString("ui-rules-wait", ("time", (int) MathF.Floor(_timer)));
        }
        else
        {
            // Timer expired — enable the accept button
            _acceptButton.Disabled = false;
            _waitLabel.Visible = false;
        }
    }
}
