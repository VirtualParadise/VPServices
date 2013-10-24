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

        public void Add(Command command)
        {
            if ( commands.Any( c => c.Name.IEquals(command.Name) ) )
                return;

            commands.Add(command);
            Log.Fine(tag, "Added command '{0}' with regex '{1}'", command.Name, command.Regex);
        }

        public void Remove(Command command)
        {
            if ( !commands.Any( c => c.Name.IEquals(command.Name) ) )
                return;

            commands.Remove(command);
            Log.Fine(tag, "Removed command '{0}' with regex '{1}'", command.Name, command.Regex);
        }

        void parse(User user, string message)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
                return;

            var cmd    = match.Groups["cmd"].Value;
            var data   = match.Groups["data"].Value;
            var target = get(cmd);

            if (target == null)
            {
                VPServices.Messages.Send(user, Colors.Warn, "Invalid command; try !help");
                return;
            }

            foreach (var command in commands)
                if ( TRegex.IsMatch(cmd, command.Regex) )
                {
                    Log.Fine(tag, "User '{0}' SID#{1} firing command '{2}'", user.Name, user.Session, command.Name);
                            
                    var success = command.Handler(user, data);
                    if (!success)
                    {
                        VPServices.Messages.Send(user, Colors.Warn, "Invalid command use; please see example:");
                        VPServices.Messages.Send(user, Colors.Warn, "", command.Example);
                    }

                    return;
                }

            Log.Debug(tag, "Unknown: {0}", cmd);
            return;
        }

        Command get(string needle)
        {
            var query = from   c in commands
                        where  TRegex.IsMatch(needle, c.Regex)
                        select c;

            return query.FirstOrDefault();
        }
    }    
}
