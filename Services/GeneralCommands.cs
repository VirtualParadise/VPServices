using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Handles general utility commands
    /// </summary>
    public class GeneralCommands : IService
    {
        enum offsetBy
        {
            X, Y, Z
        }

        const string msgCommandTitle   = "*** {0}";
        const string msgCommandRgx     = "Regex: {0}";
        const string msgCommandDesc    = "Description: {0}";
        const string msgCommandExample = "Example: {0}";

        public string Name { get { return "General commands"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Services: Help", @"^(help|commands|\?)$", cmdHelp,
                    @"Prints the URL to this documentation to chat or explains a specific command",
                    @"!help `[command]`", 5
                ),

                new Command
                (
                    "Info: Position", "^(my)?(coord(s|inates)?|pos(ition)?)$", cmdCoords,
                    @"Prints user's position to chat, including coordinates, pitch and yaw",
                    @"!pos", 5
                ),

                new Command
                (
                    "Teleport: Offset X", "^x$",
                    (s,a,d) => { return cmdOffset(a, d, offsetBy.X); },
                    @"Offsets user's X coordinate by *x* number of meters",
                    @"!x `x`"
                ),

                new Command
                (
                    "Teleport: Offset altitude", "^(alt|y)$",
                    (s,a,d) => { return cmdOffset(a, d, offsetBy.Y); },
                    @"Offsets user's altitude by *x* number of meters",
                    @"!alt `x`"
                ),

                new Command
                (
                    "Teleport: Offset Z", "^z$",
                    (s,a,d) => { return cmdOffset(a, d, offsetBy.Z); },
                    @"Offsets user's Z coordinate by *x* number of meters",
                    @"!z `z`"
                ),

                new Command
                (
                    "Teleport: Random", "^rand(om)?$", cmdRandomPos,
                    @"Teleports user to a random X,Z coordinate within the 65535 / -65535 range at ground level",
                    @"!rand"
                ),

                new Command
                (
                    "Teleport: Ground zero", "^g(round)?z(ero)?$", cmdGroundZero,
                    @"Teleports user to ground zero (0,0,0)",
                    @"!gz"
                ),

                new Command
                (
                    "Debug: Crash", "^(crash|exception)$", cmdCrash,
                    @"Crashes the bot for debugging purposes; owner only",
                    @"!crash"
                ),

                new Command
                (
                    "Debug: Hang", "^hang$", cmdHang,
                    @"Hangs the bot for debugging purposes; owner only",
                    @"!hang"
                ),
            });

            app.Routes.Add(new WebRoute("Help", "^(help|commands)$", webHelp,
                @"Provides documentation on using the bot in-world via chat"));
        }

        public void Dispose() { }

        bool cmdHelp(VPServices app, Avatar who, string data)
        {
            var helpUrl = app.PublicUrl + "help";

            if (data != "")
            {
                // If given data, try to find specific command and print help in console for
                // that user
                foreach (var cmd in app.Commands)
                    if ( TRegex.IsMatch(data, cmd.Regex) )
                    {
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgCommandTitle, cmd.Name);
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgCommandRgx, cmd.Regex);
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgCommandDesc, cmd.Help);
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgCommandExample, cmd.Example);

                        return true;
                    }

                app.Warn(who.Session, "Could not match any command for '{0}'; try {1}", data, helpUrl);
                return true;
            }
            else
            {
                // Broadcast help URL for everybody
                app.NotifyAll("Command help can be found at {0}", helpUrl);
                return true;
            }
        }

        #region Debug commands
        bool cmdCrash(VPServices app, Avatar who, string data)
        {
            var owner = app.NetworkSettings.Get("Username");

            if ( !who.Name.IEquals(owner) )
                return false;
            else
                throw new Exception("Forced crash");
        }

        bool cmdHang(VPServices app, Avatar who, string data)
        {
            var owner = app.NetworkSettings.Get("Username");

            if ( !who.Name.IEquals(owner) )
                return false;
            else
            {
                var test = 0;
                while (true) test++;
            }
        }
        #endregion

        #region Teleport commands
        bool cmdCoords(VPServices app, Avatar who, string data)
        {
            app.Notify(who.Session, "You are at {0:f4}, {1:f4}, {2:f4}, facing {3:f0}, pitch {4:f0}", who.Name, who.X, who.Y, who.Z, who.Yaw, who.Pitch);
            return true;
        }

        bool cmdOffset(Avatar who, string data, offsetBy offsetBy)
        {
            float   by;
            Vector3 location;
            if ( !float.TryParse(data, out by) )
                return false;

            switch (offsetBy)
            {
                default:
                case offsetBy.X:
                    location = new Vector3(who.X + by, who.Y, who.Z);
                    break;
                case offsetBy.Y:
                    location = new Vector3(who.X, who.Y + by, who.Z);
                    break;
                case offsetBy.Z:
                    location = new Vector3(who.X, who.Y, who.Z + by);
                    break;
            }

            VPServices.App.Bot.Avatars.Teleport(who.Session, "", location, who.Yaw, who.Pitch);
            return true;
        }

        bool cmdRandomPos(VPServices app, Avatar who, string data)
        {
            var randX = VPServices.Rand.Next(-65535, 65535);
            var randZ = VPServices.Rand.Next(-65535, 65535);

            app.Notify(who.Session, "Teleporting to {0}, 0, {1}", randX, randZ);
            app.Bot.Avatars.Teleport(who.Session, "", new Vector3(randX, 0, randZ), who.Yaw, who.Pitch);
            return true;
        } 

        bool cmdGroundZero(VPServices app, Avatar who, string data)
        {
            app.Bot.Avatars.Teleport(who.Session, "", AvatarPosition.GroundZero);
            return true;
        } 
        #endregion

        #region Web routes
        string webHelp(VPServices app, string data)
        {
            string listing = "# Bot commands available:\n";

            foreach (var command in app.Commands)
            {
                listing +=
@"## {0}

* **Regex:** {1}
* **Example:** {2}
* **Time limit:** {3}
* *{4}*

".LFormat(command.Name, command.Regex, command.Example, command.TimeLimit, command.Help);
            }

            return app.MarkdownParser.Transform(listing);
        }
        #endregion
    }
}
