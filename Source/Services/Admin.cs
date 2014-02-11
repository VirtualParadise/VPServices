using System.Text.RegularExpressions;
using System;
using System.Linq;

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
                new Command("Add world", "^addworld", (w,d) => onWorld(w,d,true),
                    "Adds a world for VPServices to begin servicing. Admin users only.",
                    "!addworld World"),

                new Command("Remove world", "^(del(ete)?|rem(ove)?)world", (w,d) => onWorld(w,d,false),
                    "Stops VPServices from servicing the given world. Admin users only.",
                    "!delworld World"),

                new Command("Load service", "^loadservice", (w,d) => onService(w,d,true),
                    "Loads a service for Services to provide. Admin users only.",
                    "!loadservice Service"),

                new Command("Unload service", "^unloadservice", (w,d) => onService(w,d,false),
                    "Unloads a service from being provided by Services. Admin users only.",
                    "!unloadservice Service"),

                new Command("Exit", "^exit$", onExit,
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

        bool onWorld(User who, string data, bool adding)
        {
            if ( !who.HasRight(Rights.Admin) )
            {
                VPServices.Messages.Send(who, Colors.Warn, "You do not have the right to use that command");
                return true;
            }

            if ( string.IsNullOrWhiteSpace(data) )
                return false;

            if (adding)
            {
                if ( VPServices.Worlds.Add(data) )
                    VPServices.Messages.Send(who, Colors.Info, "World '{0}' was added", data);
                else
                    VPServices.Messages.Send(who, Colors.Warn, "That world is already being serviced");
            }
            else
            {
                if ( VPServices.Worlds.Remove(data) )
                    VPServices.Messages.Send(who, Colors.Info, "World '{0}' was removed", data);
                else
                    VPServices.Messages.Send(who, Colors.Warn, "That world is not being serviced");
            }

            return true;
        }

        bool onService(User who, string data, bool loading)
        {
            if ( !who.HasRight(Rights.Admin) )
            {
                VPServices.Messages.Send(who, Colors.Warn, "You do not have the right to use that command");
                return true;
            }

            if ( string.IsNullOrWhiteSpace(data) )
                return false;

            var service = VPServices.Services.Get(data);

            if (service == null)
            {
                VPServices.Messages.Send(who, Colors.Warn, "That service does not exist");
                return true;
            }

            if (loading)
            {
                if ( VPServices.Services.Load(data) )
                    VPServices.Messages.Send(who, Colors.Info, "Service '{0}' was loaded", service.Name);
                else
                    VPServices.Messages.Send(who, Colors.Warn, "That service is already loaded");
            }
            else
            {
                if (VPServices.Services.Unload(data) )
                    VPServices.Messages.Send(who, Colors.Info, "Service '{0}' was removed", service.Name);
                else
                    VPServices.Messages.Send(who, Colors.Warn, "That service is not loaded");
            }

            return true;
        }

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
            if ( !( who.HasRight(Rights.Moderator) || who.HasRight(Rights.Admin) ) )
            {
                VPServices.Messages.Send(who, Colors.Warn, "You need to be a moderator+ to use that command");
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
            var loaded   = VPServices.Services.GetAllLoaded();

            foreach (var svc in services)
            {
                var extra = loaded.Contains(svc)
                    ? "and loaded"
                    : "but unloaded";

                Console.WriteLine("Service '{0}' is available {1}", svc.Name, extra);
            }
            
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
