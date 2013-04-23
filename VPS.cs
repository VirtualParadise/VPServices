using System;
using System.Threading;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static VPServices App;
        public static Random     Rand      = new Random();
        public static Color      ColorInfo = new Color(50,50,100);

        public Instance Bot;

        static void Main(string[] args)
        {
            // Set up logger
            new ConsoleLogger();

            // Handle crashes by restarting bot instance
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

            // Connect to network
            ConnectToUniverse();
            Log.Info("Network", "Connected to universe");

            // Set up global events
            SetupWeb();
            SetupCommands();
            SetupUserSettings();
            LoadServices();
            ConnectToWorld();
            Log.Info("Network", "Connected to {0}", World);

            Bot.ConsoleBroadcast(ChatTextEffect.None, ColorInfo,"", "Services is now online; say !help for information");
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
        }

        /// <summary>
        /// Pumps bot events
        /// </summary>
        public void UpdateLoop()
        {
            Bot.Wait(0);
            Thread.Sleep(100);
        }
    }
}
