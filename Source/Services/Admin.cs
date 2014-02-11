using System.Text.RegularExpressions;
using System;

namespace VPServices.Services
{
    public class Admin : IService
    {
        public string Name
        {
            get { return "Admin"; }
        }

        public Command[] Commands
        {
            get { return new[] {
                new Command("Exit", "exit", onExit,
                "Causes VPServices to fully exit from all worlds. Admin users only.")
                {
                    Enabled = bool.Parse( VPServices.Services.GetSettings(this)["CanExit"] ?? "true" )
                },

                new Command("Debug", "debug", onDebug,
                "Prints a full debug report in the console. Moderator users only."),
            }; }
        }

        public void Load() { }

        public void Unload() { }

        bool onExit(User who, string data)
        {
            if ( who.HasRight(Rights.Admin) )
                VPServices.Exit();
            else
                VPServices.Messages.Send(who, Colors.Warn, "You do not have the right to use that command");

            return true;
        }

        bool onDebug(User who, string data)
        {
            if ( !who.HasRight(Rights.Moderator) )
            {
                VPServices.Messages.Send(who, Colors.Warn, "You need to be a moderator to use that command");
                return true;
            }
            else
                VPServices.Messages.Send(who, Colors.Info, "Please see console for debug report");

            TConsole.WriteLineColored(ConsoleColor.White, ConsoleColor.Black, "### Debug report");

            TConsole.WriteLineColored(ConsoleColor.Gray, ConsoleColor.Black, "# Commands");
            var commands = VPServices.Commands.GetAll();

            foreach (var svc in commands)
            {
                Console.WriteLine("Service '{0}' provides the commands:", svc.Key.Name);

                foreach (var cmd in svc.Value)
                    Console.WriteLine("\t{0} - Regex '{1}', enabled: {2}", cmd, cmd.Regex, cmd.Enabled);
            }


            TConsole.WriteLineColored(ConsoleColor.Gray, ConsoleColor.Black, "# Services");
            var services = VPServices.Services.GetAll();

            foreach (var svc in services)
                Console.WriteLine("Service '{0}' is available", svc.Name);
            

            TConsole.WriteLineColored(ConsoleColor.Gray, ConsoleColor.Black, "# Users");
            var users = VPServices.Users.GetAll();
            
            foreach (var user in users)
            {
                Console.WriteLine("User '{0}@{1}' SID#{2}", user, user.World, user.Session);
                Console.WriteLine("Position {0}, settings:", user.Avatar.Position);
                var settings = user.GetSettings();

                foreach (var setting in settings)
                    Console.WriteLine("\t{0} - {1}", setting.Key, setting.Value);
            }


            TConsole.WriteLineColored(ConsoleColor.Gray, ConsoleColor.Black, "# Worlds");
            var worlds = VPServices.Worlds.GetAll();
            
            foreach (var world in worlds)
            {
                Console.WriteLine("{0} - State: {1}, enabled: {2}", world, world.State, world.Enabled);
                Console.WriteLine("\tLast attempted connect: {0}", world.LastAttempt);
                Console.WriteLine("\tLast successful connect: {0}", world.LastConnect);
            }

            return true;
        }
    }
}
