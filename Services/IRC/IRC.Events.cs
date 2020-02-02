using VpNet;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        void setupEvents(VPServices app, Instance bot)
        {
            // VP (outgoing) events
            app.AvatarEnter += onWorldEnter;
            app.AvatarLeave += onWorldLeave;
            bot.OnChatMessage += onWorldChat;
            //app.Chat        += onWorldChat;
            
            setupIrcEvents();

            // IRC error events
            irc.OnConnectionError += onIRCConnError;
        }

        private void setupIrcEvents()
        {
            // IRC (incoming) events
            irc.OnChannelMessage += onIRCMessage;
            irc.OnChannelAction += onIRCAction;
            irc.OnJoin += onIRCJoin;
            irc.OnPart += onIRCPart;
            irc.OnQuit += onIRCQuit;
            irc.OnKick += onIRCKick;
            irc.OnBan += onIRCBan;
            irc.OnNickChange += onIRCNick;
        }

        void onIRCConnError(object sender, System.EventArgs e)
        {
            VPServices.App.WarnAll(msgUnexpectedDisconnect);
        }
    }
}
