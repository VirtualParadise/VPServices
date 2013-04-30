using System;
using System.Text.RegularExpressions;
using System.Threading;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static VPServices App;
        public static Random     Rand    = new Random();

        public static Color ColorInfo  = new Color(50,50,100);
        public static Color ColorWarn  = new Color(220,80,20);
        public static Color ColorAlert = new Color(255,0,0);

        public Instance Bot;

        public static void Main(string[] args)
        {
            // Set up logger
            new ConsoleLogger();

            init:
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
                App.Dispose();
                goto init;
            }
        }

        /// <summary>
        /// Sets up bot instance
        /// </summary>
        public void Setup()
        {
            // Set logging level
            LogLevels logLevel;
            Enum.TryParse<LogLevels>( CoreSettings.Get("LogLevel", "Production"), out logLevel );
            Log.LogLevel = logLevel;

            // Load instance
            Bot      = new Instance( CoreSettings.Get("Name", defaultName) );
            userName = NetworkSettings.Get("Username");
            password = NetworkSettings.Get("Password");
            World    = NetworkSettings.Get("World");

            // Setup database

            // Connect to network
            ConnectToUniverse();
            Log.Info("Network", "Connected to universe");

            // Set up global events
            SetupWeb();
            SetupCommands();
            SetupUserSettings();
            ConnectToWorld();
            LoadServices();
            Log.Info("Network", "Connected to {0}", World);

            Bot.ConsoleBroadcast(ChatEffect.None, ColorInfo,"", "Services is now online; say !help for information");
        }

        /// <summary>
        /// Pumps bot events
        /// </summary>
        public void UpdateLoop()
        {
            Bot.Wait(0);
            Thread.Sleep(100);
        }

        /// <summary>
        /// Disposes of application by clearing all loaded service and disposing of bot
        /// </summary>
        public void Dispose()
        {
            Settings.Save();
            UserSettings.Save();

            Server.Abort();
            Server.Close();
            Commands.Clear();
            ClearServices();
            Bot.Dispose();
            Connection.Close();
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
            Bot.ConsoleMessage(session, ChatEffect.Italic, ColorInfo, Bot.Name, msg, parts);
        }

        public void NotifyAll(string msg, params object[] parts)
        {
            Notify(0, msg, parts);
        }

        public void Alert(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, ChatEffect.Bold, ColorAlert, Bot.Name, msg, parts);
        }

        public void AlertAll(string msg, params object[] parts)
        {
            Alert(0, msg, parts);
        }

        public void Warn(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, ChatEffect.Italic, ColorWarn, Bot.Name, msg, parts);
        }

        public void WarnAll(string msg, params object[] parts)
        {
            Warn(0, msg, parts);
        }
        #endregion
    }
}
