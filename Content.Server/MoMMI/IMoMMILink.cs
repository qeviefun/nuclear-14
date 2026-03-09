namespace Content.Server.MoMMI
{
    public interface IMoMMILink
    {
        void SendOOCMessage(string sender, string message);
        void SendAdminChatMessage(string sender, string message);
    }
}
