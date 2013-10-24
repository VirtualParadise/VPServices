using System;

namespace VPServices
{
    public class VPServices
    {
        const string tag = "VPServices";

        public static readonly CommandManager  Commands = new CommandManager();
        public static readonly DataManager     Data     = new DataManager();
        public static readonly MessageManager  Messages = new MessageManager();
        public static readonly ServiceManager  Services = new ServiceManager();
        public static readonly SettingsManager Settings = new SettingsManager();
        public static readonly UserManager     Users    = new UserManager();
        public static readonly WorldManager    Worlds   = new WorldManager();

        public static string Name
        {
            get { return Settings.Network.Get("Name"); }
        }

        static bool exiting;

        public static void Main(string[] args)
        {
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);

            try { Setup(args); }
            catch (Exception e)
            {
                Log.Severe(tag, "Failure in setup phase");
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
            Users.Setup();
            Messages.Setup();
            Commands.Setup();
            Services.Setup();
            Worlds.Setup();
        }

        public static void Loop()
        {
            while (!exiting)
            {
                Worlds.Update();
            }
        }

        public static void Exit()
        {
            exiting = true;

            Users.Takedown();
            Worlds.Takedown();

            Services.Takedown();
            Commands.Takedown();
            Data.Takedown();
            Messages.Takedown();
        }
    }
}
