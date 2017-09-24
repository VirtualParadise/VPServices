using VpNet;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        // Colors
        static Color colorChat = new Color(120, 120, 120);

		// Messages
        const string msgConnecting   = "Establishing bridge between {0} and {1} on {2}";
        const string msgDisconnected = "Disconnected bridge between {0} and {1} on {2}";
        const string msgEntry        = "*** {0} has entered {1}";
        const string msgPart         = "*** {0} has left {1}";
        const string msgQuit         = "*** {0} has quit IRC ({1})";
        const string msgNick         = "*** {0} changed their nick to {1}";
        const string msgKicked       = "*** {0} kicked {1} from {2} ({3})";
        const string msgBanned       = "*** {0} banned {1} from {2}";
        const string msgMuteUser     = "IRC chat from {0} are now {1}";
        const string msgMuteIRC      = "IRC chat is now {0} you";
        const string msgMuted        = "That IRC user is {0} muted";

		// Errors
		const string msgAlreadyConnected     = "Already connected to {0}@{1}";
		const string msgNotConnected         = "Not connected to IRC";
        const string msgUnexpectedDisconnect = "IRC has been unexpectedly disconnected; please reconnect manually with !ircconnect";
        const string msgConnectError         = "Error whilst connecting: {0}";
        const string msgJoinError            = "Error joining channel: {0}";
        
		// Tokens
        const string settingMuteList = "IRCMuteList";
        const string settingMuteIRC  = "IRCMute";
    }
}
