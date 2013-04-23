using Nini.Config;
using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Handles home setting / teleport and bouncing
    /// </summary>
    public class Home : IService
    {
        const string settingHome   = "Home";
        const string settingBounce = "Bounce";

        public string Name { get { return "Home"; } }
        public void Init(VPServices app, Instance bot)
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
            // Do not teleport users home within 10 seconds of bot's startup
            if (VPServices.App.StartUpTime.SecondsToNow() < 10)
                return;
            
            IConfig settings;
            var inst = VPServices.App;
            var user = inst.GetUser(who.Session);

            if (user == null)
                return;
            else             
                settings = inst.GetUserSettings(user);

            // Do not teleport home if bouncing
            if      ( settings.Contains(settingBounce) )
                settings.Remove(settingBounce);
            else if ( settings.Contains(settingHome) )
                cmdGoHome(inst, user, null);
            
        }

        void cmdGoHome(VPServices serv, Avatar who, string data)
        {
            var home   = serv.GetUserSettings(who).Get(settingHome);
            var pos    = (home == null)
                ? AvatarPosition.GroundZero
                : new AvatarPosition(home);

            serv.Bot.Avatars.Teleport(who.Session, "", pos);
            Log.Debug(Name, "Teleported {0} home at {1:f3}, {2:f3}, {3:f3}", who.Name, pos.X, pos.Y, pos.Z);
        }

        void cmdSetHome(VPServices serv, Avatar who, string data)
        {
            serv.GetUserSettings(who)
                .Set( settingHome, who.Position.ToString() );

            serv.Bot.Say("{0}: Set your home to {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
            Log.Info(Name, "Set home for {0} at {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
        }

        void cmdClearHome(VPServices serv, Avatar who, string data)
        {
            var  config = serv.GetUserSettings(who);
            if ( !config.Contains(settingHome) )
                return;
            else
                config.Remove(settingHome);

            serv.Bot.Say("{0}: Cleared; home assumed as 0,0,0", who.Name);
            Log.Info(Name, "Cleared home for {0}", who.Name);
        }

        void cmdBounce(VPServices serv, Avatar who, string data)
        {
            serv.GetUserSettings(who)
                .Set(settingBounce, true);

            serv.Bot.Avatars.Teleport(who.Session, serv.World, who.Position);
            Log.Info(Name, "Bounced user {0}", who.Name);
        }
    }
}
