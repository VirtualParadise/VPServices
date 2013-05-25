using IrcDotNet;
using Nini.Config;
using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Provides a VP <> IRC bridge
    /// </summary>
    class IRC : IService
    {
        static Color  colorChat = new Color(120,120,120);
        const  string msgEntry  = "*** {0} has entered {1}";
        const  string msgPart   = "*** {0} has left {1}";
        const  string msgQuit   = "*** {0} has quit IRC ({1})";
        const  char   ircAction = (char) 0x01;

        VPServices app;
        IrcClient  irc;
        IConfig    config;
        string     channel;
        string     host;
        int        port;
        
        public string Name { get { return "IRC"; } }
        public void   Init (VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "IRC: Connect", "^irc(start|connect)$",
                    (s,a,d) => { return connect(); },
                    @"Starts the IRC-VP bridge", "!ircstart", 60
                ),
                
                new Command
                (
                    "IRC: Disconnect", "^irc(end|disconnect)$",
                    (s,a,d) => { return disconnect(); },
                    @"Stops the IRC-VP bridge", "!ircend", 60
                )
            });


            config = app.Settings.Configs["IRC"] ?? app.Settings.Configs.Add("IRC");

            this.app  = app;
            host      = config.Get("Server", "irc.ablivion.net");
            port      = config.GetInt("Port", 6667);
            channel   = config.Get("Channel", "#vp");
            app.Chat += onWorldChat;
            
            if (config.GetBoolean("Autoconnect", false))
                connect();
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        #region Dis/connection logic
        bool connect()
        {
            if ( irc != null && irc.IsConnected )
            {
                app.WarnAll("IRC is already connected");
                Log.Warn(Name, "Rejecting connection attempt; already connected");
                return true;
            }

            if ( irc != null )
                disconnect();

            app.NotifyAll("Establishing bridge between {0} and {1} on {2}", app.World, channel, host);
            Log.Info(Name, "Creating and establishing IRC bridge...");

            irc = new IrcClient();
            var reg = new IrcUserRegistrationInfo
            {
                NickName = config.Get("Nickname", "VPBridgeBot"),
                RealName = config.Get("Realname", "VPBridge Admin"),
                UserName = config.Get("Username", "VPBridgeAdmin"),
            };

            irc.Error                += (o, e) => { Log.Warn(Name, e.Error.ToString()); };
            irc.ErrorMessageReceived += (o, e) => { Log.Warn(Name, "IRC error: {0}", e.Message); };
            irc.ProtocolError        += (o, e) => { Log.Warn(Name, "Protocol error: {0} {1}", e.Code, e.Message); };
            irc.RawMessageReceived   += onIRCMessage;

            irc.Connect(host, port, false, reg);
            irc.Registered   += onIRCConnected;
            irc.Disconnected += onIRCDisconnected;
            return true;
        }

        bool disconnect()
        {
            if ( irc == null || !irc.IsConnected )
            {
                app.WarnAll("No IRC connection to disconnect");
                Log.Warn(Name, "Rejecting disconnect attempt; no IRC connection");
                return true;
            }

            Log.Info(Name, "Disconnecting and disposing IRC bridge...");
            irc.Disconnected -= onIRCDisconnected;
            irc.Disconnect();
            irc.Dispose();
            irc = null;
            return true;
        } 
        #endregion

        #region Event handlers
        void onWorldChat(Instance sender, Avatar user, string message)
        {
            if ( irc == null || !irc.IsConnected )
                return;

            var msg = "PRIVMSG {2} :{0}: {1}".LFormat(user.Name, message, channel);
            irc.SendRawMessage(msg);
        }

        void onIRCConnected(object sender, EventArgs e)
        {
            irc.Channels.Join(channel);
        }

        void onIRCDisconnected(object sender, EventArgs e)
        {
            app.NotifyAll("IRC bridge has been disconnected");
            Log.Warn(Name, "Disconnected from server; reconnecting...");
            disconnect();
            connect();
        }

        void onIRCMessage(object sender, IrcRawMessageEventArgs e)
        {
            if ( config.GetBoolean("DebugProtocol", false) )
                Log.Fine(Name, "Protocol message: {0}", e.RawContent);

            var bot = app.Bot;
            if ( e.Message.Parameters[0] == channel )
            {
                if ( e.Message.Command.IEquals("PRIVMSG") )
                {
                    var msg = e.Message.Parameters[1];

                    if (msg[0] == ircAction)
                    {
                        msg = msg.Trim(ircAction);
                        msg = msg.Remove(0, 7);
                        bot.ConsoleBroadcast(ChatEffect.None, colorChat, "", "{0} {1}", e.Message.Source.Name, msg);
                    }
                    else
                        bot.ConsoleBroadcast(ChatEffect.None, colorChat, e.Message.Source.Name, msg);
                }
                else if ( e.Message.Command.IEquals("JOIN") )
                    bot.ConsoleBroadcast(ChatEffect.Italic, VPServices.ColorInfo, "", msgEntry, e.Message.Source.Name, channel);
                else if ( e.Message.Command.IEquals("PART") )
                    bot.ConsoleBroadcast(ChatEffect.Italic, VPServices.ColorInfo, "", msgPart, e.Message.Source.Name, channel);
            }
            else if ( e.Message.Command.IEquals("QUIT") )
                bot.ConsoleBroadcast(ChatEffect.Italic, VPServices.ColorInfo, "", msgQuit, e.Message.Source.Name, e.Message.Parameters[0]);
        } 
        #endregion

    }
}
