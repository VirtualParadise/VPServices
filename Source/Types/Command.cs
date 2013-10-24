using System;
using VP;

namespace VPServices
{
    public delegate bool CommandHandler(User who, string data);

    /// <summary>
    /// Defines a text command, fired by !(regex)
    /// </summary>
    public class Command
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

        public Command(string name, string rgx, CommandHandler handler, string help, string example = "")
        {
            Name      = name;
            Regex     = rgx;
            Handler   = handler;
            Help      = help;
            Example   = example;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
