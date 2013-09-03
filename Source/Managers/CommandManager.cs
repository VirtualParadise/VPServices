using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VP;

namespace VPServices
{
    public class CommandManager
    {
        const string tag     = "Commands";
        const string pattern = "^!(?<cmd>[a-z]+)( (?<data>.+))?$";

        List<Command> list = new List<Command>();

        public void Setup()
        {
            VPServices.Worlds.Added   += w => { w.Bot.Chat += parse; };
            VPServices.Worlds.Removed += w => { w.Bot.Chat -= parse; };
        }

        void parse(Instance bot, ChatMessage chat)
        {
            var match = Regex.Match(chat.Message, pattern, RegexOptions.IgnoreCase);
            var user  = VPServices.Users.BySession(chat.Session);

            if (user == null || !match.Success)
                return;

            var cmd    = match.Groups["cmd"].Value;
            var data   = match.Groups["data"].Value;
            var target = get(cmd);

            if (target == null)
            {
                App.Warn(user.Session, "Invalid command; try !help");
                return;
            }

            foreach (var cmd in list)
                if ( TRegex.IsMatch(invocation, cmd.Regex) )
                {
                    Log.Fine(tag, "User '{0}' firing command '{1}'", chat.Name, cmd.Name);
                    cmd.LastInvoked = DateTime.Now;
                            
                    var success = cmd.Handler(this, user, data);
                    if (!success)
                    {
                        App.Warn(user.Session, "Invalid command use; please see example:");
                        Bot.ConsoleMessage(user.Session, ChatEffect.Italic, ColorWarn, "", cmd.Example);
                    }

                    return;
                }

            Log.Debug("Commands", "Unknown: {0}", invocation);
            return;
        }

        Command get(string needle)
        {
            var query = from   c in list
                        where  TRegex.IsMatch(needle, c.Regex)
                        select c;

            return query.FirstOrDefault();
        }
    }

    
}
