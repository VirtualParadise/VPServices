using VPServices;

namespace VPServices.Services
{
	partial class IRC : IService
    {
        void setupEvents(VPServices app)
        {
            // VP (outgoing) events
            app.AvatarEnter += onWorldEnter;
            app.AvatarLeave += onWorldLeave;
            app.Chat        += onWorldChat;

            // IRC (incoming) events
            irc.OnChannelMessage += onIRCMessage;
            irc.OnChannelAction  += onIRCAction;
            irc.OnJoin           += onIRCJoin;
            irc.OnPart           += onIRCPart;
            irc.OnQuit           += onIRCQuit;
            irc.OnKick           += onIRCKick;
            irc.OnBan            += onIRCBan;
            irc.OnNickChange     += onIRCNick;

            // IRC error events
            irc.OnConnectionError += onIRCConnError;
        }

        void onIRCConnError(object sender, System.EventArgs e)
        {
            VPServices.App.WarnAll(msgUnexpectedDisconnect);
        }
    }
}
