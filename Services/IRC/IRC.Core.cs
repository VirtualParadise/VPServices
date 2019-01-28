using Meebey.SmartIrc4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VpNet;

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
            
            setupEvents(app, bot);
            loadSettings(app);
            this.app = app;

            // Auto-connect IRC asynchronously if set
            if ( config.AutoConnect )
                Task.Factory.StartNew(() => { connect(app); });
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        #region Privates
        VPServices app;
        IrcClient  irc   = new IrcClient();
        object     mutex = new object();
        IConfiguration iniConfig;

        IrcConfig _config;
        /// <summary>
        /// Thread-safe setter for IRC config
        /// </summary>
        IrcConfig config
        {
            get { return _config; }
            set { lock (mutex) { _config = value; } }
        }
        #endregion

        #region Settings logic
        void loadSettings(VPServices app)
        {
            lock (mutex)
            {
                iniConfig = app.Settings.GetSection("IRC");

                config = new IrcConfig
                {
                    Host    = iniConfig.GetValue("Server", "localhost"),
                    Port    = iniConfig.GetValue("Port", 6667),
                    Channel = iniConfig.GetValue("Channel", "#vp"),

                    AutoConnect = iniConfig.GetValue("Autoconnect", false),
                    NickName    = iniConfig.GetValue("Nickname", "VPBridgeBot"),
                    RealName    = iniConfig.GetValue("Realname", "VPBridgeAdmin"),
                };

                Log.Debug(Name, "Loaded IRC connection settings");
            }
        } 
        #endregion

		#region Dis/connection logic
        void connect(VPServices app)
        {
            lock (mutex)
            {
                app.NotifyAll(msgConnecting, app.World, config.Channel, config.Host);
                Log.Info(Name, "Creating and establishing IRC bridge...");

                try
                {
                    irc.Connect(config.Host, config.Port);
                    irc.Login(config.NickName, config.RealName);

                    Log.Debug(Name, "Connected and logged into {0}", config.Host);
                }
                catch (Exception e)
                {
                    // Ensure disconnection
                    if (irc.IsConnected)
                        irc.Disconnect();

                    app.WarnAll(msgConnectError, e.Message);
                    Log.Warn(Name, "Could not login to IRC: {0}", e.Message);
                    return;
                }
                
                try
                {
                    irc.RfcJoin(config.Channel);

                    Log.Debug(Name, "Joined channel {0}", config.Channel);
                }
                catch (Exception e)
                {
                    // Ensure disconnection
                    if (irc.IsConnected)
                        irc.Disconnect();

                    app.WarnAll(msgConnectError, e.Message);
                    Log.Warn(Name, "Could not join channel: {0}", e.Message);
                    return;
                }

                // Start IRC task
                Task.Factory.StartNew(updateLoop);
            }
        }

        void updateLoop()
        {
            while (irc.IsConnected)
                try
                {
                    irc.ListenOnce();
                }
                catch (Exception e)
                {
                    Log.Warn(Name, "Exception in IRC listen loop: {0}", e.Message);
                    return;
                }
        }

        void disconnect(VPServices app)
        {
            lock (mutex)
            {
                if (!irc.IsConnected)
                    return;

                app.NotifyAll(msgDisconnected, app.World, config.Channel, config.Host);
                Log.Info(Name, "Disconnecting IRC bridge...");

                irc.RfcQuit("Goodbye");
                irc.Disconnect();
                Log.Debug(Name, "Disconnected IRC bridge");
            }
        } 
        #endregion
    }

	struct IrcConfig
    {
		public string Host;
		public int    Port;
		public string Channel;
		public string NickName;
		public string RealName;

		public bool AutoConnect;
    }
}
