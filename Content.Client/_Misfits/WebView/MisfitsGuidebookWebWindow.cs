// #Misfits Add - WebView-backed guidebook window that opens the live Misfits wiki
using System.Numerics;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Misfits.WebView;

/// <summary>
/// Floating guidebook window powered by CEF WebView showing the live Misfits wiki.
/// Replaces the legacy XAML tree-based <c>GuidebookWindow</c>.
/// </summary>
public sealed class MisfitsGuidebookWebWindow : DefaultWindow
{
    private const string WikiMainPageUrl = "https://ss14.misfitsystems.net/wiki/index.php/Main_Page";

    protected override Vector2 ContentsMinimumSize => new Vector2(900f, 650f);

    private readonly MisfitsWebViewControl _webView;

    public MisfitsGuidebookWebWindow()
    {
        Title = Loc.GetString("guidebook-window-title");

        _webView = new MisfitsWebViewControl
        {
            HorizontalExpand = true,
            VerticalExpand = true
        };
        _webView.NavigateTo(WikiMainPageUrl);

        Contents.AddChild(_webView);
    }

    /// <summary>Navigate the embedded wiki to a specific page URL.</summary>
    public void OpenUrl(string url)
    {
        _webView.NavigateTo(url);
    }
}
