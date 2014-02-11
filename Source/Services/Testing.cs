using System;
using System.Text.RegularExpressions;

namespace VPServices.Services
{
    public class Testing : IService
    {
        public string Name
        {
            get { return "Testing"; }
        }

        public Command[] Commands
        {
            get { return new[] {
                new Command("Message", "msg", onMsg,
                    "Sends a cross-world message to all sessions of a given name",
                    "!msg Target: Hello world!"),

                new Command("Test", "test", onTest,
                    "This command should be disabled") { Enabled = false },

                new Command("Crash", "crash", onCrash,
                    "This command should crash the commandmanager"),
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
                VPServices.Messages.Send(who, Colors.Warn, "User '{0}' is not online in any worlds I am servicing", target);
            else
            {
                VPServices.Messages.Send(who, Colors.Info, "Message has been sent to {0} sessions of user '{1}'", targetUser.Length, targetUser[0]);

                foreach (var user in targetUser)
                {
                    VPServices.Messages.Send(user, Colors.Info, "You have a message from user {0}@{1}:", who, who.World);
                    VPServices.Messages.Send(user, Colors.Info, "\"{0}\"", message);
                }
            }

            return true;
        }

        bool onTest(User who, string data)
        {
            throw new InvalidOperationException("This command is supposed to be disabled");
        }

        bool onCrash(User who, string data)
        {
            if ( !who.HasRight(Rights.Admin) )
            {
                VPServices.Messages.Send(who, Colors.Warn, "You do not have the right to use that command");
                return true;
            }

            throw new Exception("Forced crash");
        }
    }
}
