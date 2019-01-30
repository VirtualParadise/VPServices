using Serilog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        readonly ILogger commandsLogger = Log.ForContext("Tag", "Commands");
        /// <summary>
        /// Global list of all commands registered
        /// </summary>
        public SortedSet<Command> Commands = new SortedSet<Command>();

        /// <summary>
        /// Sets up event handlers for command parsing and chat printing to console
        /// </summary>
        public void SetupCommands()
        {
            //Chat += parseCommand;
            //Chat += (s, a, m) =>
            //{
            //    TConsole.WriteLineColored(ConsoleColor.White, " {0} | {1}", a.Name.PadRight(16), m);
            //};

            Bot.OnChatMessage += (s, c) =>
            {
                if (string.IsNullOrWhiteSpace(c.ChatMessage.Name))
                {
                    TConsole.WriteLineColored(ConsoleColor.White, "Console: {0}", c.ChatMessage.Message);
                }
                else
                {
                    parseCommand(s, c.Avatar, c.ChatMessage.Message);
                    TConsole.WriteLineColored(ConsoleColor.White, " {0} | {1}", c.ChatMessage.Name.PadRight(16), c.ChatMessage.Message);
                }
            };

            //Bot. += (s, c) =>
            //{
            //    TConsole.WriteLineColored(ConsoleColor.White, "Console: {0} {1}", c.Name, c.Message);
            //};
        }

        /// <summary>
        /// Parses incoming chat for a command and runs it
        /// </summary>
        void parseCommand(Instance sender, Avatar<Vector3> user, string message)
        {
            // Accept only commands
            if ( !message.StartsWith("!") )
                return;

            var intercept = message
                .Trim()
                .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            var beginTime     = DateTime.Now;
            var targetCommand = intercept[0].Substring(1).ToLower();
            var data          =
                intercept.Length == 2
                ? intercept[1].Trim()
                : "";

            // Iterate through commands, rejecting invokes if time limited
            foreach (var cmd in Commands)
                if ( TRegex.IsMatch(targetCommand, cmd.Regex) )
                {
                    var timeSpan = cmd.LastInvoked.SecondsToNow();
                    if (timeSpan < cmd.TimeLimit)
                    {
                        App.Warn(user.Session, "That command was used too recently; try again in {0} seconds.", cmd.TimeLimit - timeSpan);
                        commandsLogger.Information("User {User} tried to invoke {Command} too soon", user.Name, cmd.Name);
                    }
                    else
                    {
                        try
                        {
                            commandsLogger.Information("User {User} firing command {Command}", user.Name, cmd.Name);
                            cmd.LastInvoked = DateTime.Now;
                            
                            var success = cmd.Handler(this, user, data);
                            if (!success)
                            {
                                App.Warn(user.Session, "Invalid command use; please see example:");
                                Bot.ConsoleMessage(user.Session, "", cmd.Example, ColorWarn, TextEffectTypes.Italic);
                            }
                        }
                        catch (Exception e)
                        {
                            App.Alert(user.Session, "Sorry, I ran into an issue executing that command. Please notify the host.");
                            App.Alert(user.Session, "Error: {0}", e.Message);

                            commandsLogger.Error(e, "Exception firing command {0}", cmd.Name);
                        }
                    }

                    commandsLogger.Information("Command {Command} took {Duration} seconds to process", cmd.Name, beginTime.SecondsToNow());
                    return;
                }

            App.Warn(user.Session, "Invalid command; try !help");
            commandsLogger.Debug("Unknown: {0}", targetCommand);
            return;
        }
    }

    public delegate bool CommandHandler(VPServices app, Avatar<Vector3> who, string data);

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
        /// Example string for this command
        /// </summary>
        public string Example;
        /// <summary>
        /// How many seconds after invoking is this command disabled
        /// </summary>
        public int TimeLimit;
        /// <summary>
        /// Timestamp command was last invoked
        /// </summary>
        public DateTime LastInvoked = DateTime.Now.AddSeconds(-9001);

        public Command(string name, string rgx, CommandHandler handler, string help, string example = "", int timeLimit = 0)
        {
            Name      = name;
            Regex     = rgx;
            Handler   = handler;
            Help      = help;
            Example   = example;
            TimeLimit = timeLimit;
        }

        public int CompareTo(Command other) { return this.Name.CompareTo(other.Name); }
    }
}
