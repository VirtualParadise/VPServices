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
                "Causes VPServices to fully exit from all worlds"),

                new Command("Message", "msg", onMsg,
                "Sends a cross-world message to all sessions of a given name",
                "!msg Target: Hello world!"),

                new Command("Debug", "debug", onDebug,
                "Prints a full debug report in the console"),

                new Command("Test", "test", onDebug,
                "This command should be disabled") { Enabled = false },
            }; }
        }

        public void Load() { }

        public void Unload() { }

        static bool onExit(User who, string data)
        {
            VPServices.Exit();
            return true;
        }

        static bool onMsg(User source, string data)
        {
            var matches = Regex.Match(data, "^(?<who>.+?): (?<msg>.+)$");
            
            if (!matches.Success)
                return false;

            var target     = matches.Groups["who"].Value;
            var message    = matches.Groups["msg"].Value;
            var targetUser = VPServices.Users.ByName(target);

            if (targetUser.Length == 0)
                VPServices.Messages.Send(source, Colors.Warn, "User '{0}' is not online in any worlds I am servicing", target);
            else
            {
                VPServices.Messages.Send(source, Colors.Info, "Message has been sent to {0} sessions of user '{1}'", targetUser.Length, targetUser[0]);

                foreach (var user in targetUser)
                {
                    VPServices.Messages.Send(user, Colors.Info, "You have a message from user {0}@{1}:", source, source.World);
                    VPServices.Messages.Send(user, Colors.Info, "\"{0}\"", message);
                }
            }

            return true;
        }

        static bool onDebug(User source, string data)
        {
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
