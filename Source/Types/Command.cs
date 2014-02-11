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
        /// Gets the canonical name of this command
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Gets the regex pattern that matches this command
        /// </summary>
        public readonly string Regex;
        /// <summary>
        /// Gets the handler to call when this command is invoked
        /// </summary>
        public readonly CommandHandler Handler;
        /// <summary>
        /// Gets the help string for this command
        /// </summary>
        public readonly string Help;
        /// <summary>
        /// Gets the example string for this command
        /// </summary>
        public readonly string Example;
        /// <summary>
        /// Gets or sets if this command is enabled
        /// </summary>
        public bool Enabled = true;

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
