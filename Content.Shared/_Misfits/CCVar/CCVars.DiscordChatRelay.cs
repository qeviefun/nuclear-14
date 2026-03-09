using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// URL of the Discord webhook which will relay in-game OOC messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordOOCWebhook =
        CVarDef.Create("discord.ooc_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// URL of the Discord webhook which will relay in-game admin chat messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordAdminChatWebhook =
        CVarDef.Create("discord.admin_chat_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}