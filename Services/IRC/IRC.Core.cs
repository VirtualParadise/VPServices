using Meebey.SmartIrc4net;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VpNet;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        readonly ILogger logger = Log.ForContext("Tag", "IRC");
		public string Name
        { 
            get { return "IRC"; }
        }

        public void Init (VPServices app, Instance bot)
        {
            app.Commands.Add(new Command(
                "IRC: Connect", "^irc(start|connect)$", cmdIRCConnect,
                @"Starts the IRC-VP bridge", "!ircstart"
            ));
                
            app.Commands.Add(new Command(
                "IRC: Disconnect", "^irc(end|disconnect)$", cmdIRCDisconnect,
                @"Stops the IRC-VP bridge", "!ircend"
            ));

            app.Commands.Add(new Command(
                "IRC: Mute", "^ircmute$",
                (s, w, d) => { return cmdMute(s, w, d, true); },
                @"Mutes specific user or all of IRC for you", "!ircmute `[target]`"
            ));

            app.Commands.Add(new Command(
                "IRC: Unmute", "^ircunmute$",
                (s, w, d) => { return cmdMute(s, w, d, false); },
                @"Unmutes specific user or all of IRC for you", "!ircunmute `[target]`"
            ));
            
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

                logger.Debug("Loaded IRC connection settings");
            }
        } 
        #endregion

		#region Dis/connection logic
        void connect(VPServices app)
        {
            lock (mutex)
            {
                app.NotifyAll(msgConnecting, app.World, config.Channel, config.Host);
                logger.Information("Creating and establishing IRC bridge...");

                try
                {
                    irc.Connect(config.Host, config.Port);
                    irc.Login(config.NickName, config.RealName);

                    logger.Debug("Connected and logged into {Host}", config.Host);
                }
                catch (Exception e)
                {
                    // Ensure disconnection
                    if (irc.IsConnected)
                        irc.Disconnect();

                    app.WarnAll(msgConnectError, e.Message);
                    logger.Warning(e, "Could not login to IRC: {Error}", e.Message);
                    return;
                }
                
                try
                {
                    irc.RfcJoin(config.Channel);

                    logger.Debug("Joined channel {Channel}", config.Channel);
                }
                catch (Exception e)
                {
                    // Ensure disconnection
                    if (irc.IsConnected)
                        irc.Disconnect();

                    app.WarnAll(msgConnectError, e.Message);
                    logger.Warning(e, "Could not join channel: {Error}", e.Message);
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
                    logger.Warning(e, "Exception in IRC listen loop: {Error}", e.Message);
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
                logger.Information("Disconnecting IRC bridge...");

                irc.RfcQuit("Goodbye");
                irc.Disconnect();
                logger.Debug("Disconnected IRC bridge");
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
