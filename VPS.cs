using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static VPServices App;
        public static Random     Rand = new Random();

        public static Color ColorLesser = new Color(150,150,200);
        public static Color ColorInfo   = new Color(50,50,100);
        public static Color ColorWarn   = new Color(220,80,20);
        public static Color ColorAlert  = new Color(255,0,0);

        public Instance Bot;
        public string   Owner;
        public bool     Crash;

        public static int Main(string[] args)
        {
            int exit = 0;

            // Set up logger
            new ConsoleLogger();
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);

            try
            {
                App = new VPServices();
                App.SetupSettings(args);
                App.Setup();

                while (true)
                    App.UpdateLoop();
            }
            catch (Exception e)
            {
                e.LogFullStackTrace();
                exit = 1;
            }
            finally
            {
                App.Dispose();
            }

            Console.WriteLine("### [{0}] Services is now exiting", DateTime.Now);
            return exit;
        }

        /// <summary>
        /// Sets up bot instance
        /// </summary>
        public void Setup()
        {
            // Set logging level
            LogLevels logLevel;
            Enum.TryParse<LogLevels>( CoreSettings.GetValue("LogLevel", "Production"), out logLevel );
            Log.Level = logLevel;

            // Load instance
            //Bot = new Instance(new InstanceConfiguration<World>()
            //{
            //    BotName =
            //}); CoreSettings.Get("Name", defaultName) );

            botName = CoreSettings.GetValue("Name", defaultName);
            userName = NetworkSettings.GetValue("Username", "");
            password = NetworkSettings.GetValue("Password", "");
            World = NetworkSettings.GetValue("World", "");
            Owner = userName;

            Bot = new Instance();

            // Connect to network
            ConnectToUniverse();
            Log.Info("Network", "Connected to universe");

            // Set up subsystems
            SetupDatabase();
            SetupWeb();
            SetupCommands();
            SetupEvents();
            LoadServices();

            // Set up services
            ConnectToWorld();
            PerformMigrations();
            InitServices();
            Log.Info("Network", "Connected to {0}", World);

            //TODO: Save this somewhere else?
            //CoreSettings.Set("Version", MigrationVersion);
            var result = Bot.ConsoleMessage("", "Services is now online; say !help for information", ColorInfo);
        }

        /// <summary>
        /// Pumps bot events
        /// </summary>
        public void UpdateLoop()
        {
            if (Crash)
            {
                Crash = false;
                throw new Exception("Forced crash in update loop");
            }

            Thread.Sleep(100);
        }

        /// <summary>
        /// Disposes of application by clearing all loaded service and disposing of bot
        /// </summary>
        public void Dispose()
        {
            Commands.Clear();

            ClearWeb();
            ClearEvents();
            ClearServices();
            CloseDatabase();
            Bot.Dispose();
        }

        #region Helper functions
        public static bool TryParseBool(string msg, out bool value)
        {
            if ( TRegex.IsMatch(msg, "^(true|1|yes|on)$") )
            {
                value = true;
                return true;
            }
            else if ( TRegex.IsMatch(msg, "^(false|0|no|off)$") )
            {
                value = false;
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }

        public void Notify(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, Bot.Configuration.BotName, string.Format(msg, parts), ColorInfo, TextEffectTypes.Italic);
        }

        public void NotifyAll(string msg, params object[] parts)
        {
            Notify(0, msg, parts);
        }

        public void Alert(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, Bot.Configuration.BotName, string.Format(msg, parts), ColorAlert, TextEffectTypes.Bold);
        }

        public void AlertAll(string msg, params object[] parts)
        {
            Alert(0, msg, parts);
        }

        public void Warn(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, Bot.Configuration.BotName, string.Format(msg, parts), ColorWarn, TextEffectTypes.Italic);
        }

        public void WarnAll(string msg, params object[] parts)
        {
            Warn(0, msg, parts);
        }
        #endregion
    }
}
