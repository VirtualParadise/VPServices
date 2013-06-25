using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Threading;
using VP;

namespace VPServices.Services
{
	partial class IRC : IService
    {
		public string Name
        { 
            get { return "IRC"; }
        }

        public void Init (VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "IRC: Connect", "^irc(start|connect)$", cmdIRCConnect,
                    @"Starts the IRC-VP bridge", "!ircstart"
                ),
                
                new Command
                (
                    "IRC: Disconnect", "^irc(end|disconnect)$", cmdIRCDisconnect,
                    @"Stops the IRC-VP bridge", "!ircend"
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

            
            setupEvents(app);
            loadSettings(app);

            if ( config.AutoConnect )
                connect(app);
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        #region Privates
        IRCState  state = IRCState.Disconnected;
        IrcClient irc   = new IrcClient();
        object    mutex = new Mutex();
        IrcConfig config;
        Thread    thread;
        #endregion

        #region Settings logic
        void loadSettings(VPServices app)
        {
            var ini = app.Settings.Configs["IRC"] ?? app.Settings.Configs.Add("IRC");

            config = new IrcConfig
            {
                Host    = ini.Get("Server", "localhost"),
                Port    = ini.GetInt("Port", 6667),
                Channel = ini.Get("Channel", "#vp"),

                AutoConnect   = ini.GetBoolean("Autoconnect", false),
                DebugProtocol = ini.GetBoolean("DebugProtocol", false),

                NickName = ini.Get("Nickname", "VPBridgeBot"),
                RealName = ini.Get("Realname", "VPBridge Admin"),
                UserName = ini.Get("Username", "VPBridgeAdmin"),
            };

            Log.Debug(Name, "Loaded IRC connection settings");
        } 
        #endregion

		#region Dis/connection logic
        void connect(VPServices app)
        {
			state = IRCState.Connecting;
            app.NotifyAll(msgConnecting, app.World, config.Channel, config.Host);
            Log.Info(Name, "Creating and establishing IRC bridge...");

                        
            irc.Connect(config.Host, config.Port, false, config.Registration);
        }

        void disconnect(VPServices app)
        {
			state = IRCState.Disconnecting;

            Log.Info(Name, "Disconnecting IRC bridge...");
            irc.Quit(10000, "Goodbye");
        } 
        #endregion
    }

	enum IRCState
    {
        Disconnecting,
        Disconnected,
        Connecting,
        Connected
    }

	struct IrcConfig
    {
		public string Host;
		public int    Port;
		public string Channel;
		public string NickName;
		public string RealName;
		public string UserName;

		public bool AutoConnect;
		public bool DebugProtocol;
    }
}
