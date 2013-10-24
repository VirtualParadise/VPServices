using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VPServices
{
    public class CommandManager
    {
        const string tag     = "Commands";
        const string pattern = "^!(?<cmd>[a-z]+)( (?<data>.+))?$";

        List<Command> commands = new List<Command>();

        public void Setup()
        {
            VPServices.Messages.Incoming += parse;
        }

        public void Takedown()
        {
            commands.Clear();
            Log.Debug(tag, "All commands cleared");
        }

        public void Add(params Command[] toAdd)
        {
            foreach (var command in toAdd)
            {
                if ( commands.Any( c => c.Name.IEquals(command.Name) ) )
                    return;

                commands.Add(command);
                Log.Fine(tag, "Added command '{0}' with regex '{1}'", command, command.Regex);
            }
        }

        public void Remove(Command command)
        {
            if ( !commands.Any( c => c.Name.IEquals(command.Name) ) )
                return;

            commands.Remove(command);
            Log.Fine(tag, "Removed command '{0}' with regex '{1}'", command, command.Regex);
        }

        void parse(User user, string message)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
                return;

            var cmd    = match.Groups["cmd"].Value;
            var data   = match.Groups["data"].Value;
            var target = commands.Where( c => TRegex.IsMatch(cmd, c.Regex) ).FirstOrDefault();

            if (target == null)
                return;

            Log.Fine(tag, "User '{0}' SID#{1} firing command '{2}'", user, user.Session, target);
                            
            var success = target.Handler(user, data);
            if (!success)
            {
                VPServices.Messages.Send(user, Colors.Warn, "Invalid command use; please see example:");
                VPServices.Messages.Send(user, Colors.Warn, "", target.Example);
            }
        }
    }    
}
