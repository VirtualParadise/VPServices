using IrcDotNet;
using Nini.Config;
using System;
using System.Linq;
using System.Collections.Generic;
using VP;
using SQLite;

namespace VPServices.Services
{
    /// <summary>
    /// Provides a VP <> IRC bridge
    /// </summary>
    partial class IRC : IService
    {
        public string Name
        { 
            get { return "IRC"; }
        }

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
                ),

                new Command
                (
                    "IRC: Mute", "^ircmute$",
                    (s, w, d) => { return cmdMute(s, w, d, true); },
                    @"Mutes specific user or all of IRC for you", "!ircmute `[target]`"
                ),

                new Command
                (
                    "IRC: Unmute", "^ircunmute$",
                    (s, w, d) => { return cmdMute(s, w, d, false); },
                    @"Unmutes specific user or all of IRC for you", "!ircunmute `[target]`"
                ),
            });

            this.app  = app;
            config    = app.Settings.Configs["IRC"] ?? app.Settings.Configs.Add("IRC");
            host      = config.Get("Server", "irc.ablivion.net");
            port      = config.GetInt("Port", 6667);
            channel   = config.Get("Channel", "#vp");
            app.Chat += onWorldChat;
            
            if (config.GetBoolean("Autoconnect", false))
                connect();
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        #region Privates and strings
        static Color  colorChat   = new Color(120, 120, 120);
        const  string msgEntry    = "*** {0} has entered {1}";
        const  string msgPart     = "*** {0} has left {1}";
        const  string msgQuit     = "*** {0} has quit IRC ({1})";
        const  string msgMuteUser = "IRC chat from {0} are now {1}";
        const  string msgMuteIRC  = "IRC chat is now {0} you";
        const  string msgMuted    = "That IRC user is {0} muted";
        const  char   ircAction   = (char) 0x01;

        const string settingMuteList = "IRCMuteList";
        const string settingMuteIRC  = "IRCMute";

        SQLiteConnection connection;
        VPServices       app;
        IrcClient        irc;
        IConfig          config;
        string           channel;
        string           host;
        int              port; 
        #endregion

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

        #region Command handlers
        bool cmdMute(VPServices app, Avatar who, string target, bool muting)
        {
            // Mute IRC
            if (target == "")
                return toggleIRC(who, muting);

            if ( target.Contains(',') )
            {
                app.Warn(who.Session, "Cannot mute that name; commas not allowed");
                return true;
            }

            var muteList = who.GetSetting(settingMuteList);
            var muted    = ( muteList ?? "" ).TerseSplit(',').ToList();

            if (muting)
            {
                if ( muted.Contains(target) )
                {
                    app.Warn(who.Session, msgMuted, "already");
                    return true;
                }
                
                muted.Add(target);
                app.Notify(who.Session, msgMuteUser, target, "hidden");
            }
            else
            {
                if ( !muted.Contains(target) )
                {
                    app.Warn(who.Session, msgMuted, "not");
                    return true;
                }
                
                muted.Remove(target);
                app.Notify(who.Session, msgMuteUser, target, "shown");
            }

            muteList = string.Join(",", muted);
            who.SetSetting(settingMuteList, muteList);
            return true;
        }

        bool toggleIRC(Avatar who, bool muting)
        {
            who.SetSetting(settingMuteIRC, muting);
            var msg = muting ? "hidden from" : "shown to";
            app.Notify(who.Session, msgMuteIRC, msg);

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
                        broadcast(false, "", "{0} {1}", e.Message.Source.Name, msg);
                    }
                    else
                        broadcast(false, e.Message.Source.Name, msg);
                }
                else if ( e.Message.Command.IEquals("JOIN") )
                    broadcast(true, "", msgEntry, e.Message.Source.Name, channel);
                else if ( e.Message.Command.IEquals("PART") )
                    broadcast(true, "", msgPart, e.Message.Source.Name, channel);
            }
            else if ( e.Message.Command.IEquals("QUIT") )
                broadcast(true, "", msgQuit, e.Message.Source.Name, e.Message.Parameters[0]);
        }

        void broadcast(bool announce, string name, string message, params object[] parts)
        {
            var fx    = announce ? ChatEffect.Italic : ChatEffect.None;
            var color = announce ? VPServices.ColorInfo : colorChat;

            foreach (var user in VPServices.App.Users)
            {
                // No broadcasting to those muting IRC
                if ( user.GetSettingBool(settingMuteIRC) )
                    continue;

                var muteList = user.GetSetting(settingMuteList);
                var muted    = ( muteList ?? "" ).TerseSplit(',').ToList();

                // No broadcasting to those muting target user
                if ( muted.Contains(name) )
                    continue;

                VPServices.App.Bot.ConsoleMessage(user.Session, fx, color, name, message, parts);
            }
        }
        #endregion

    }
}
