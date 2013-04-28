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
        const string settingBounce = "bounce";
        const string settingHome   = "Home";

        public string Name { get { return "Home"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Home: Set", "^sethome$", cmdSetHome,
                    @"Sets user's home position, where they will be teleported to every time they enter the world",
                    @"!sethome"
                ),

                new Command
                (
                    "Home: Teleport", "^home$", cmdGoHome,
                    @"Teleports user to their home position, or ground zero if unset",
                    @"!home"
                ),

                new Command
                (
                    "Home: Clear", "^clearhome$", cmdClearHome,
                    @"Clears user's home position",
                    @"!clearhome"
                ),

                new Command
                (
                    "Teleport: Bounce", "^bounce$", cmdBounce,
                    @"Disconnects and reconnects user to the world; useful for clearing the download queue and fixing some issues",
                    @"!bounce"
                ),
            });

            bot.Avatars.Enter += onEnter;
        }

        public void Dispose() { }

        #region Command handlers
        bool cmdGoHome(VPServices app, Avatar who, string data)
        {
            var home = app.GetUserSettings(who).Get(settingHome);
            var pos  = ( home == null )
                ? AvatarPosition.GroundZero
                : new AvatarPosition(home);

            app.Bot.Avatars.Teleport(who.Session, "", pos);
            return Log.Debug(Name, "Teleported {0} home at {1:f3}, {2:f3}, {3:f3}", who.Name, pos.X, pos.Y, pos.Z);
        }

        bool cmdSetHome(VPServices app, Avatar who, string data)
        {
            var pos = who.Position.ToString();
            app.GetUserSettings(who).Set(settingHome, pos);

            app.Notify(who.Session, "Set your home to {0:f3}, {1:f3}, {2:f3}" , who.X, who.Y, who.Z);
            return Log.Info(Name, "Set home for {0} at {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
        }

        bool cmdClearHome(VPServices app, Avatar who, string data)
        {
            var  config = app.GetUserSettings(who);
            if ( config.Contains(settingHome) )
            {
                config.Remove(settingHome);
                app.Notify(who.Session, "Your home has been cleared to ground zero");
            }
            else
                app.Notify(who.Session, "You do not have a home location");

            return Log.Info(Name, "Cleared home for {0}", who.Name);
        }

        bool cmdBounce(VPServices app, Avatar who, string data)
        {
            app.GetUserSettings(who).Set(settingBounce, true);
            app.Bot.Avatars.Teleport(who.Session, app.World, who.Position);

            return Log.Info(Name, "Bounced user {0}", who.Name);
        } 
        #endregion

        #region Event handlers
        void onEnter(Instance sender, Avatar who)
        {
            // Do not teleport users home within 10 seconds of bot's startup
            if ( VPServices.App.StartUpTime.SecondsToNow() < 10 )
                return;

            IConfig settings;
            var inst = VPServices.App;
            var user = inst.GetUser(who.Session);

            if ( user == null )
                return;
            else
                settings = inst.GetUserSettings(user);

            // Do not teleport home if bouncing
            if      ( settings.Contains(settingBounce) )
                settings.Remove(settingBounce);
            else if ( settings.Contains(settingHome) )
                cmdGoHome(inst, user, null);
        }
        #endregion
    }
}
