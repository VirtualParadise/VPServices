using IrcDotNet;
using System;
using VP;

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
            irc.Registered         += onIRCRegistered;
            irc.ConnectFailed      += onIRCConnectFailed;
            irc.Disconnected       += onIRCDisconnected;
            irc.RawMessageReceived += onIRCMessage;

            // IRC errors
            irc.Error                += onIRCError;
            irc.ErrorMessageReceived += (o, e) => { Log.Warn(Name, "IRC error: {0}", e.Message); };
            irc.ProtocolError        += (o, e) => { Log.Warn(Name, "Protocol error: {0} {1}", e.Code, e.Message); };
        }

		#region Dis/connection event handlers
		/// <summary>
		/// Received when connected to server; needed to subsequently join channel
		/// </summary>
        void onIRCRegistered(object sender, EventArgs e)
        {
            irc.Channels.Join(config.Channel);
            state = IRCState.Connected;
        }

        void onIRCConnectFailed(object sender, IrcErrorEventArgs e)
        {
            VPServices.App.AlertAll(msgConnectError, e.Error.Message);
            irc.Disconnect();
        }

        void onIRCDisconnected(object sender, EventArgs e)
        {
            switch (state)
            {
                /// Does not appear to fire for any connection errors...
                case IRCState.Connected:
                    VPServices.App.WarnAll(msgUnexpectedDisconnect);
                    break;

                case IRCState.Connecting:
                    Log.Warn(Name, "Got disconnection event whilst connecting");
                    break;

                /// When receiving disconnected event during this state, IRC is actually
                /// quitting but not fully disconnected. Bug?
                case IRCState.Disconnecting:
                    VPServices.App.NotifyAll(msgDisconnected, VPServices.App.World, config.Channel, config.Host);
                    irc.Disconnect();
                    break;

                /// Disconnected proper
                case IRCState.Disconnected:
                    Log.Debug(Name, "Disconnection complete");
                    break;
            }

            state = IRCState.Disconnected;
        }

		void onIRCError(object sender, IrcErrorEventArgs e)
        {
			switch (state)
            {
                case IRCState.Connected:
                    irc.Disconnect();
                    break;

                case IRCState.Connecting:
                    VPServices.App.AlertAll(msgConnectError, e.Error.Message);
                    irc.Disconnect();
                    break;

                case IRCState.Disconnecting:
                    VPServices.App.AlertAll(msgDisconnectError, e.Error.Message);
                    state = IRCState.Connected;
                    break;
            }

			Log.Severe(Name, "Error whilst in state '{0}':", state);
			Log.Severe(Name, e.Error.Message);
        }
        #endregion
    }
}
