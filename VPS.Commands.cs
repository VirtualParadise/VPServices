using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VP;

namespace VPServ
{
    public partial class VPServ : IDisposable
    {
        /// <summary>
        /// Global list of all commands registered
        /// </summary>
        public SortedSet<Command> Commands = new SortedSet<Command>();

        /// <summary>
        /// Sets up event handlers for command parsing and chat printing to console
        /// </summary>
        public void SetupCommands()
        {
            Bot.Chat += parseCommand;
            Bot.Chat += (s, c) =>
            {
                Log.Debug(c.Name, c.Message.Replace('{','[').Replace('}',']'));
            };
        }

        void parseCommand(Instance sender, Chat chat)
        {
            var beginTime = DateTime.Now;
            // Accept only commands
            if (!chat.Message.StartsWith("!")) return;

            var intercept = chat.Message
                .Trim()
                .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            var user = GetUser(chat.Session);
            var targetCommand = intercept[0].Substring(1).ToLower();
            var data = intercept.Length == 2
                ? intercept[1].Trim()
                : "";

            // Reject bots
            if (user.IsBot) return;

            // Iterate through commands, rejecting invokes if time limited
            foreach (var cmd in Commands)
                if (TBXRegex.IsMatch(targetCommand, cmd.Regex))
                {
                    if (DateTime.Now.Subtract(cmd.LastInvoked).TotalSeconds < cmd.TimeLimit)
                        Log.Info("Commands", "User {0} tried to invoke {1} too soon", user.Name, cmd.Name);
                    else
                    {
                        try
                        {
                            Log.Fine("Commands", "User {0} firing command {1}", user.Name, cmd.Name);
                            cmd.LastInvoked = DateTime.Now;
                            cmd.Handler(this, user, data);
                        }
                        catch (Exception e)
                        {
                            Log.Severe("Commands", "Exception firing command {0}", cmd.Name);
                            e.LogFullStackTrace();
                        }
                    }

                    Log.Fine("Commands", "Command {0} took {1} seconds to process", cmd.Name, DateTime.Now.Subtract(beginTime).TotalSeconds);
                    return;
                }

            Log.Debug("Commands", "Unknown: {0}", targetCommand);
            Log.Fine("Commands", "Took {0} seconds to process",DateTime.Now.Subtract(beginTime).TotalSeconds);
            return;
        }
    }

    public delegate void CommandHandler(VPServ serv, Avatar who, string data);

    /// <summary>
    /// Defines a text command, fired by !(regex)
    /// </summary>
    public class Command : IComparable<Command>
    {
        /// <summary>
        /// Canonical command name
        /// </summary>
        public string Name;
        /// <summary>
        /// Regex pattern that matches this command
        /// </summary>
        public string Regex;
        /// <summary>
        /// Handler to call when this command is invoked
        /// </summary>
        public CommandHandler Handler;
        /// <summary>
        /// Help string for this command
        /// </summary>
        public string Help;
        /// <summary>
        /// How many seconds after invoking is this command disabled
        /// </summary>
        public int TimeLimit;
        /// <summary>
        /// Timestamp command was last invoked
        /// </summary>
        public DateTime LastInvoked = DateTime.MinValue;

        public Command(string name, string rgx, CommandHandler handler, string help, int timeLimit = 0)
        {
            Name = name;
            Regex = rgx;
            Handler = handler;
            Help = help;
            TimeLimit = timeLimit;
        }

        public int CompareTo(Command other) { return this.Name.CompareTo(other.Name); }
    }
}
