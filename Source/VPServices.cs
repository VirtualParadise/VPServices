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

        static string[] args;

        static bool exiting;
        static bool restarting;

        static void Main(string[] args)
        {
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);
            Log.QuickSetup();
            VPServices.args = args;

        begin:
            try { setup(); }
            catch (Exception e)
            {
                Log.Severe(tag, "Failure in setup phase");
                Log.LogFullStackTrace(e);
                Exit();
            }

            if (!exiting)
                try { loop(); }
                catch (Exception e)
                {
                    Log.Severe(tag, "Failure in the main loop");
                    Log.LogFullStackTrace(e);
                }

            Log.Info(tag, "Services is now exiting...");
            takedown();
            Console.WriteLine("### [{0}] Services is finished", DateTime.Now);

            if (restarting)
            {
                restarting = false;
                exiting   = false;

                Console.WriteLine("\n\n### [{0}] Services is reloading...", DateTime.Now);
                goto begin;
            }
        }

        static void setup()
        {
            Settings.Setup(args);
            Data.Setup();
            Users.Setup();
            Messages.Setup();
            Commands.Setup();
            Services.Setup();
            Worlds.Setup();
        }

        static void loop()
        {
            while (!exiting && !restarting)
                Worlds.Update();
        }

        static void takedown()
        {
            Users.Takedown();
            Worlds.Takedown();

            Services.Takedown();
            Commands.Takedown();
            Data.Takedown();
            Messages.Takedown();
            Log.Debug(tag, "Full takedown complete");
        }

        public static void Exit()
        {
            exiting = true;
        }

        public static void Restart()
        {
            restarting = true;
        }
    }
}
