using Nini.Config;
using System;
using System.Collections.Generic;
using VP;

namespace VPServ.Services
{
    /// <summary>
    /// Handles home setting / teleport and bouncing
    /// </summary>
    public class Home : IService
    {
        public const string SETTING_HOME   = "Home";
        public const string SETTING_BOUNCE = "bounce";

        public string Name { get { return "Home"; } }
        public void Init(VPServ app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command("Set home", "^sethome$", cmdSetHome,
                @"Sets the requester's home position, where they will be teleported to every time they enter the world", 30),

                new Command("Go home", "^h(ome)?$", cmdGoHome,
                @"Teleports the requester to their home position, or ground zero if unset"),

                new Command("Clear home", "^clearhome$", cmdClearHome,
                @"Clears the requester's home position"),

                new Command("Bounce", "^bounce$", cmdBounce,
                @"Disconnects and reconnects the requester to the world; useful for clearing the download queue and fixing some issues"),
            });

            bot.Avatars.Enter += onEnter;
        }

        public void Dispose() { }

        void onEnter(Instance sender, Avatar who)
        {
            // Teleport home or prev position on bounce, but not on 10 seconds of initial connection
            if (DateTime.Now.Subtract(VPServ.Instance.StartUpTime).TotalSeconds > 10)
            {
                IConfig settings;
                var inst = VPServ.Instance;
                var user = inst.GetUser(who.Session);

                if (user == null) return;
                else              settings = inst.GetUserSettings(user);

                // Do not teleport home if bouncing
                if (settings.Contains(SETTING_BOUNCE))
                    settings.Remove(SETTING_BOUNCE);
                else if (settings.Contains(SETTING_HOME))
                    cmdGoHome(inst, user, null);
            }
        }

        void cmdGoHome(VPServ serv, Avatar who, string data)
        {
            var home = serv.GetUserSettings(who).Get(SETTING_HOME);
            var pos = (home == null)
                ? new AvatarPosition(0, 0, 0, 0, 0)
                : new AvatarPosition(home);

            serv.Bot.Avatars.Teleport(who.Session, "", pos);
            Log.Debug(Name, "Teleported {0} home at {1:f3}, {2:f3}, {3:f3}", who.Name, pos.X, pos.Y, pos.Z);
        }

        void cmdSetHome(VPServ serv, Avatar who, string data)
        {
            serv.GetUserSettings(who).Set(SETTING_HOME, who.Position.ToString());

            serv.Bot.Say("{0}: Set your home to {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
            Log.Info(Name, "Set home for {0} at {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
        }

        void cmdClearHome(VPServ serv, Avatar who, string data)
        {
            if (!serv.GetUserSettings(who).Contains(SETTING_HOME)) return;
            else serv.GetUserSettings(who).Remove(SETTING_HOME);

            serv.Bot.Say("{0}: Cleared; home assumed as 0,0,0", who.Name);
            Log.Info(Name, "Cleared home for {0}", who.Name);
        }

        void cmdBounce(VPServ serv, Avatar who, string data)
        {
            serv.GetUserSettings(who).Set(SETTING_BOUNCE, true);
            serv.Bot.Avatars.Teleport(who.Session, serv.World, who.Position);
            Log.Info(Name, "Bounced user {0}", who.Name);
        }
    }
}
