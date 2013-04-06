using System;
using System.Collections.Generic;
using VP;
using IrcDotNet;
using Nini.Config;

namespace VPServ.Services
{
    /// <summary>
    /// Provides a VP <> IRC bridge
    /// </summary>
    class IRC : IService
    {
        IrcClient irc;
        IConfig   config;
        string    channel;
        string    host;
        int       port;
        
        public string Name { get { return "IRC"; } }
        public void   Init (VPServ app, Instance bot)
        {
            app.Commands.Add(new Command("IRC connect", "^irc(start|connect)?$", (s,a,d) => { connect(); },
                @"Starts the IRC-VP bridge", 60));

            app.Commands.Add(new Command("IRC disconnect", "^irc(end|disconnect)?$", (s,a,d) => { disconnect(); },
                @"Stops the IRC-VP bridge", 60));

            config = app.Settings.Configs["IRC"] ?? app.Settings.Configs.Add("IRC");

            host      = config.Get("Server", "irc.ablivion.net");
            port      = config.GetInt("Port", 6667);
            channel   = config.Get("Channel", "#vp");
            bot.Chat += onWorldChat;
            
            if (config.GetBoolean("Autoconnect", false))
                connect();
        }

        void connect()
        {
            if (irc != null && irc.IsConnected)
            {
                Log.Warn(Name, "Rejecting connection attempt; already connected");
                return;
            }
            
            if (irc != null)
                disconnect();

            Log.Info(Name, "Creating and establishing IRC bridge...");

                irc = new IrcClient();
            var reg = new IrcUserRegistrationInfo
            {
                NickName = config.Get("Nickname", "VPBridgeBot"),
                RealName = config.Get("Realname", "VPBridge Admin"),
                UserName = config.Get("Username", "VPBridgeAdmin"),
            };

            irc.Error                += (o,e) => { Log.Warn(Name, e.Error.ToString()); };
            irc.ErrorMessageReceived += (o,e) => { Log.Warn(Name, "IRC error: {0}", e.Message); };
            irc.ProtocolError        += (o,e) => { Log.Warn(Name, "Protocol error: {0} {1}", e.Code, e.Message); };
            irc.RawMessageReceived   += onIRCMessage;

            irc.Connect(host, port, false, reg);
            irc.Registered   += onIRCConnected;
            irc.Disconnected += onIRCDisconnected;
        }

        void disconnect()
        {
            if (irc == null || !irc.IsConnected)
            {
                Log.Warn(Name, "Rejecting disconnect attempt; no IRC connection");
                return;
            }

            Log.Info(Name, "Disconnecting and disposing IRC bridge...");
            irc.Disconnected -= onIRCDisconnected;
            irc.Disconnect();
            irc.Dispose();
            irc = null;
        }

        void onWorldChat(Instance sender, Chat chat)
        {
            if (irc == null || !irc.IsConnected)
                return;

            var msg = string.Format("PRIVMSG {2} :{0}: {1}", chat.Name, chat.Message, channel);
            irc.SendRawMessage(msg);
        }

        void onIRCConnected(object sender, EventArgs e)
        {
            irc.Channels.Join(channel);
        }

        void onIRCDisconnected(object sender, EventArgs e)
        {
            Log.Warn(Name, "Disconnected from server; reconnecting...");
            disconnect();
            connect();
        }

        void onIRCMessage(object sender, IrcRawMessageEventArgs e)
        {
            Log.Fine(Name, "Protocol message: {0}", e.RawContent);

            if (e.Message.Parameters[0] == channel)
            {
                if      ( e.Message.Command.IEquals("PRIVMSG") )
                    VPServ.Instance.Bot.Say("/me {0}:\t{1}", e.Message.Source.Name, e.Message.Parameters[1]);
                else if ( e.Message.Command.IEquals("JOIN") )
                    VPServ.Instance.Bot.Say("/me *** {0} has joined {1}", e.Message.Source.Name, channel);
                else if ( e.Message.Command.IEquals("PART") )
                    VPServ.Instance.Bot.Say("/me *** {0} has left {1}", e.Message.Source.Name, channel);
            }
            else if ( e.Message.Command.IEquals("QUIT") )
                VPServ.Instance.Bot.Say("/me *** {0} has quit ({1})", e.Message.Source.Name, e.Message.Parameters[0]);
            
        }

        public void Dispose()
        {
            
        }
    }
}
