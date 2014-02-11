using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VPServices.Services;

namespace VPServices
{
    public class CommandManager
    {
        const string tag     = "Commands";
        const string pattern = "^!(?<cmd>[a-z]+)( (?<data>.+))?$";

        Dictionary<IService, Command[]> commands = new Dictionary<IService, Command[]>();

        public void Setup()
        {
            VPServices.Messages.Incoming += onChat;
            VPServices.Services.Loaded   += onServiceLoad;
            VPServices.Services.Unloaded += onServiceUnload;
        }

        public void Takedown()
        {
            commands.Clear();
            Log.Info(tag, "All commands cleared");
        }

        public Dictionary<IService, Command[]> GetAll()
        {
            return new Dictionary<IService, Command[]>(commands);
        }

        void onServiceLoad(IService service)
        {
            commands.Add(service, service.Commands);
            Log.Fine(tag, "Added {0} commands from loaded service '{1}'", service.Commands.Length, service.Name);
        }

        void onServiceUnload(IService service)
        {
            commands.Remove(service);
            Log.Fine(tag, "Removed {0} commands from loaded service '{1}'", service.Commands.Length, service.Name);
        }

        void onChat(User user, string message)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
                return;

            var cmd    = match.Groups["cmd"].Value;
            var data   = match.Groups["data"].Value;
            var query  = from   s in commands
                         from   c in s.Value
                         where  TRegex.IsMatch(cmd, c.Regex)
                         select c;
            var target = query.FirstOrDefault();

            if (target == null)
                return;

            if (!target.Enabled)
            {
                VPServices.Messages.Send(user, Colors.Warn, "This command has been disabled by the operator");
                Log.Debug(tag, "User '{0}' SID#{1} tried disabled command '{2}'", user, user.Session, target);
                return;
            }

            if (target.Rights != null)
            {
                var rights = user.Rights;
                var canUse = rights.Intersect(target.Rights).Any();

                if (!canUse)
                {
                    var needed = string.Join(",", target.Rights);
                    VPServices.Messages.Send(user, Colors.Warn, "You do not have the right to use that command (required: {0})", needed);
                    return;
                }
            }

            Log.Fine(tag, "User '{0}' SID#{1} firing command '{2}'", user, user.Session, target);
            
            try
            {
                var success = target.Handler(user, data);
                if (!success)
                {
                    VPServices.Messages.Send(user, Colors.Warn, "Invalid command use; please see example:");
                    VPServices.Messages.Send(user, Colors.Warn, "{0}", target.Example);
                }
            }
            catch (Exception e)
            {
                VPServices.Messages.Send(user, Colors.Alert, "Something went wrong, please notify the operator");
                Log.Severe(tag, "Error executing command '{0}' for user '{1}@{2}'", cmd, user, user.World);
                Log.LogFullStackTrace(e);
            }
        }
    }    
}
