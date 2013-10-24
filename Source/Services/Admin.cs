using System.Text.RegularExpressions;

namespace VPServices.Services
{
    public class Admin : IService
    {
        public string Name
        {
            get { return "Admin"; }
        }

        static readonly Command cmdExit = new Command("Exit", "exit", onExit,
            "Causes VPServices to fully exit from all worlds");

        static readonly Command cmdMsg = new Command("Message", "msg", onMsg,
            "Sends a cross-world message to all sessions of a given name",
            "!msg Target: Hello world!");

        public void Load()
        {
            VPServices.Commands.Add(cmdExit, cmdMsg);
        }

        public void Unload()
        {
        }

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
    }
}
