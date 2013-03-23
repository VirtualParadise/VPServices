using System;
using System.Collections.Generic;
using VP;

namespace VPServ.Services
{
    /// <summary>
    /// Handles general utility commands
    /// </summary>
    public class GeneralCommands : IService
    {
        public const string SETTING_BOUNCE = "bounce";

        public string Name { get { return "General commands"; } }
        public void Init(VPServ app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command("Help", "^(help|commands)$", (s,a,d) => { s.Bot.Say(s.PublicUrl + "help"); },
                @"Prints the URL to this documentation to chat", 60),
                new Command("Position", "^(my)?(coord(s|inates)?|pos(ition)?)$", cmdCoords,
                @"Prints the requester's position to chat, including coordinates, pitch and yaw", 5),
                new Command("Crash", "^(crash|exception)$", cmdCrash,
                @"Crashes the bot for debugging purposes; owner only"),
                new Command("Offset altitude", "^alt$", cmdOffsetAlt,
                @"Offsets the requester's altitude by *x* number of meters in the format: `!alt x`"),
                new Command("Offset X", "^x$", cmdOffsetX,
                @"Offsets the requester's X coordinate by *x* number of meters in the format: `!x x`"),
                new Command("Offset Z", "^z$", cmdOffsetZ,
                @"Offsets the requester's Z coordinate by *x* number of meters in the format: `!z x`"),
                new Command("Random position", "^rand(om)?$", cmdRandomPos,
                @"Teleports the requester to a random X,Z coordinate within the 32560 / -32560 range at ground level"),
            });

            app.Routes.Add(new WebRoute("Help", "^(help|commands)$", webHelp,
                @"Provides documentation on using the bot in-world via chat"));
        }

        public void Dispose() { }

        void cmdCrash(VPServ serv, Avatar who, string data)
        {
            var owner = serv.NetworkSettings.Get("Username");

            if ( !who.Name.Equals(owner, StringComparison.CurrentCultureIgnoreCase) )
                return;
            else
                throw new Exception("Forced crash");
        }

        void cmdCoords(VPServ serv, Avatar who, string data)
        {
            serv.Bot.Say("{0}: {1:f4}, {2:f4}, {3:f4}, facing {4:f0}, pitch {5:f0}", who.Name, who.X, who.Y, who.Z, who.Yaw, who.Pitch);
        }

        void cmdOffsetAlt(VPServ serv, Avatar who, string data)
        {
            float x;
            if (float.TryParse(data, out x))
                serv.Bot.Avatars.Teleport(who.Session, "", new Vector3(who.X, who.Y + x, who.Z), who.Yaw, who.Pitch);
        }

        void cmdOffsetX(VPServ serv, Avatar who, string data)
        {
            float x;
            if (float.TryParse(data, out x))
                serv.Bot.Avatars.Teleport(who.Session, "", new Vector3(who.X + x, who.Y, who.Z), who.Yaw, who.Pitch);
        }

        void cmdOffsetZ(VPServ serv, Avatar who, string data)
        {
            float x;
            if (float.TryParse(data, out x))
                serv.Bot.Avatars.Teleport(who.Session, "", new Vector3(who.X, who.Y, who.Z + x), who.Yaw, who.Pitch);
        }

        void cmdRandomPos(VPServ serv, Avatar who, string data)
        {
            serv.Bot.Avatars.Teleport(who.Session, "",
                new Vector3(VPServ.Rand.Next(-32750, 32750), 0, VPServ.Rand.Next(-32750, 32750))
                , who.Yaw, who.Pitch);
        }

        string webHelp(VPServ serv, string data)
        {
            string listing = "# Bot commands available:\n";

            foreach (var command in serv.Commands)
            {
                listing += string.Format(
@"## {0}

* **Regex:** {1}
* **Time limit:** {2}
* *{3}*

", command.Name, command.Regex, command.TimeLimit, command.Help);
            }

            return serv.MarkdownParser.Transform(listing);
        }
    }
}
