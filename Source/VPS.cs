using System;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static CommandManager  Commands = new CommandManager();
        public static DataManager     Data     = new DataManager();
        public static EventManager    Events   = new EventManager();
        public static ServiceManager  Services = new ServiceManager();
        public static SettingsManager Settings = new SettingsManager();
        public static UserManager     Users    = new UserManager();
        public static WorldManager    Worlds   = new WorldManager();

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
            Log.QuickSetup();

            Settings.Setup(args);
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
            
        }
    }
}
