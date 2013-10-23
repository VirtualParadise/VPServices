using System;

namespace VPServices
{
    public class VPServices
    {
        const string tag = "VPServices";

        public static readonly CommandManager  Commands = new CommandManager();
        public static readonly DataManager     Data     = new DataManager();
        public static readonly EventManager    Events   = new EventManager();
        public static readonly ServiceManager  Services = new ServiceManager();
        public static readonly SettingsManager Settings = new SettingsManager();
        public static readonly UserManager     Users    = new UserManager();
        public static readonly WorldManager    Worlds   = new WorldManager();

        static bool exiting;

        public static void Main(string[] args)
        {
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);

            try { Setup(args); }
            catch (Exception e)
            {
                Log.Severe(tag, "Services setup failed");
                Log.LogFullStackTrace(e);
                Exit();
            }
            
            if (exiting) 
                goto exit;
            
            try { Loop(); }
            catch (Exception e)
            {
                Log.Severe(tag, "Failure in the main loop");
                Log.LogFullStackTrace(e);
            }
            finally { Exit(); }

        exit:
            Console.WriteLine("### [{0}] Services is now exiting", DateTime.Now);
        }

        public static void Setup(string[] args)
        {
            Log.QuickSetup();

            Settings.Setup(args);
            Data.Setup();

            Commands.Setup();
            Events.Setup();
        }

        public static void Loop()
        {
            if (!exiting)
                Loop();
        }

        public static void Exit()
        {
            exiting = true;
        }
    }
}
