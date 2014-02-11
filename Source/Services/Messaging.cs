using System;
using System.Text.RegularExpressions;

namespace VPServices.Services
{
    public class Messaging : IService
    {
        public string Name
        {
            get { return "Messaging"; }
        }

        public Command[] Commands
        {
            get { return new[] {
                new Command("Message", "^msg", onMsg,
                    "Sends a cross-world message to all sessions of a given name",
                    "!msg Target: Hello world!"),

                new Command("Broadcast", "^broadcast", onBroadcast,
                    "Sends a broadcast message to all sessions in a given world",
                    "!broadcast Target: Hello world!")
                    {
                        Rights = new[] { Rights.Admin, Rights.Moderator }
                    },
            }; }
        }

        public void Load() { }

        public void Unload() { }

        bool onMsg(User who, string data)
        {
            var matches = Regex.Match(data, "^(?<who>.+?): (?<msg>.+)$");
            
            if (!matches.Success)
                return false;

            var target     = matches.Groups["who"].Value;
            var message    = matches.Groups["msg"].Value;
            var targetUser = VPServices.Users.ByName(target);

            if (targetUser.Length == 0)
                who.Send.Warn("User '{0}' is not online in any worlds I am servicing", target);
            else
            {
                who.Send.Info("Message has been sent to {0} session(s) of user '{1}'", targetUser.Length, targetUser[0]);

                foreach (var user in targetUser)
                {
                    user.Send.Info("You have a message from user {0}@{1}:", who, who.World);
                    user.Send.Info("\"{0}\"", message);
                }
            }

            return true;
        }

        bool onBroadcast(User who, string data)
        {
            var matches = Regex.Match(data, "^(?<world>.+?): (?<msg>.+)$");
            
            if (!matches.Success)
                return false;

            var target  = matches.Groups["world"].Value;
            var message = matches.Groups["msg"].Value;
            var world   = VPServices.Worlds.Get(target);

            if (world == null)
                who.Send.Warn("World '{0}' is not one I am servicing", target);
            else
            {
                who.Send.Info("Broadcast has been sent to world '{0}'", world);

                world.Send.Info("Broadcast sent from user {0}@{1}:", who, who.World);
                world.Send.Info("\"{0}\"", message);
            }

            return true;
        }
    }
}
