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

            Log.Fine(tag, "User '{0}' SID#{1} firing command '{2}'", user, user.Session, target);
                            
            var success = target.Handler(user, data);
            if (!success)
            {
                VPServices.Messages.Send(user, Colors.Warn, "Invalid command use; please see example:");
                VPServices.Messages.Send(user, Colors.Warn, "{0}", target.Example);
            }
        }
    }    
}
