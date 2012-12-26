using System;
using System.Threading;
using VP;

namespace VPServ
{
    public partial class VPServ : IDisposable
    {
        public static VPServ Instance;
        public static Random Rand = new Random();

        public Instance Bot;

        static void Main(string[] args)
        {
            // Set up logger
            Log.LogLevel = LogLevels.All;
            new ConsoleLogger();

            // Handle crashes by restarting bot instance
        init:
            try
            {
                Instance = new VPServ();
                Instance.Setup();
                while (true) { Instance.UpdateLoop(); }
            }
            catch (Exception e)
            {
                e.LogFullStackTrace();
                Instance.Dispose();
                goto init;
            }
        }

        /// <summary>
        /// Sets up bot instance
        /// </summary>
        public void Setup()
        {
            // Load instance
            Bot = new Instance("Services");
            SetupSettings();
            userName = NetworkSettings.Get("Username");
            password = NetworkSettings.Get("Password");
            World = NetworkSettings.Get("World");

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
            Bot.Wait(-1);
            Thread.Sleep(100);
        }
    }
}
