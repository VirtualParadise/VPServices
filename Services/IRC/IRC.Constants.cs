using IrcDotNet;
using System;
using VP;

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
        const string msgMuteUser     = "IRC chat from {0} are now {1}";
        const string msgMuteIRC      = "IRC chat is now {0} you";
        const string msgMuted        = "That IRC user is {0} muted";

		// Errors
		const string msgAlreadyConnecting    = "Already attempting to connect to {0}@{1}";
		const string msgAlreadyConnected     = "Already connected to {0}@{1}";
		const string msgNotConnected         = "Not connected to IRC";
		const string msgDisconnecting        = "Waiting for disconnection";
		const string msgCantInterrupt        = "Cannot interrupt a pending connection";
        const string msgUnexpectedDisconnect = "IRC has been unexpectedly disconnected; please retry with !ircconnect";
        const string msgConnectError         = "Error whilst connecting: {0}";
        const string msgDisconnectError      = "Error whilst disconnecting: {0}";
        
		// Tokens
        const char   ircAction		 = (char) 0x01;
        const string settingMuteList = "IRCMuteList";
        const string settingMuteIRC  = "IRCMute";

		IRCState  state = IRCState.Disconnected;
        IrcClient irc   = new IrcClient();
		IRCConfig config;
    }
}
