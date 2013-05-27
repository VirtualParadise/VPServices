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
        public string Name
        { 
            get { return "Home"; }
        }

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

            app.AvatarEnter += onEnter;
            app.AvatarLeave += onLeave;
        }

        public void Migrate(VPServices app, int target) {  }
        public void Dispose() { }

        const string settingLastExit = "LastExit";
        const string settingBounce   = "Bounce";
        const string settingHome     = "Home";

        #region Command handlers
        bool cmdGoHome(VPServices app, Avatar who, string data)
        {
            var home = who.GetSetting(settingHome);
            var pos  = ( home == null )
                ? AvatarPosition.GroundZero
                : new AvatarPosition(home);

            app.Bot.Avatars.Teleport(who.Session, "", pos);
            return Log.Debug(Name, "Teleported {0} home at {1:f3}, {2:f3}, {3:f3}", who.Name, pos.X, pos.Y, pos.Z);
        }

        bool cmdSetHome(VPServices app, Avatar who, string data)
        {
            var pos = who.Position.ToString();
            who.SetSetting(settingHome, pos);

            app.Notify(who.Session, "Set your home to {0:f3}, {1:f3}, {2:f3}" , who.X, who.Y, who.Z);
            return Log.Info(Name, "Set home for {0} at {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
        }

        bool cmdClearHome(VPServices app, Avatar who, string data)
        {
            who.DeleteSetting(settingHome);
            app.Notify(who.Session, "Your home has been cleared to ground zero");

            return Log.Info(Name, "Cleared home for {0}", who.Name);
        }

        bool cmdBounce(VPServices app, Avatar who, string data)
        {
            who.SetSetting(settingBounce, true);
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

            var lastExit = who.GetSettingDateTime(settingLastExit);

            // Ignore bouncing/disconnected users
            if ( lastExit.SecondsToNow() < 60 )
                return;

            // Do not teleport home if bouncing
            if      ( who.GetSetting(settingBounce) != null )
                who.DeleteSetting(settingBounce);
            else if ( who.GetSetting(settingHome) != null )
                cmdGoHome(VPServices.App, who, null);
        }

        void onLeave(Instance sender, Avatar who)
        {
            // Keep track of LastExit to prevent annoying users
            who.SetSetting(settingLastExit, DateTime.Now);
        }
        #endregion
    }
}
