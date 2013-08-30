using System;
using System.Text.RegularExpressions;
using System.Threading;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static DataManager     Data;
        public static SettingsManager Settings;
        public static UserManager     Users;

        static bool exiting;

        public static void Main(string[] args)
        {
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);

            Setup(args);
            Loop();
            TakeDown();

            Console.WriteLine("### [{0}] Services is now exiting", DateTime.Now);
        }

        public static void Setup(string[] args)
        {
            Log.Loggers.Add( new ConsoleLogger() );

            Settings = new SettingsManager(args);
            Data     = new DataManager();
            Users    = new UserManager();
        }

        public static void Loop()
        {
            


            if (exiting) return;
            else         Loop();
        }

        /// <summary>
        /// Disposes of application by clearing all loaded service and disposing of bot
        /// </summary>
        public static void TakeDown()
        {
            Commands.Clear();

            ClearEvents();
            ClearServices();
            CloseDatabase();
            Bot.Dispose();
        }
    }
}
