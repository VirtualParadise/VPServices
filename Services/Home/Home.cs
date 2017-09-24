using Nini.Config;
using System;
using System.Collections.Generic;
using VpNet;
using SQLite;

namespace VPServices.Services
{
    /// <summary>
    /// Handles home setting / teleport and bouncing
    /// </summary>
    public partial class Home : IService
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
                    "Home: Teleport", "^home$",
                    (s,a,d) => { cmdGoHome(s, a, false); return true; },
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
            this.connection = app.Connection;
        }

        public void Dispose() { }

        SQLiteConnection connection;

        const string settingLastExit = "LastExit";
        const string settingBounce   = "Bounce";
        const string settingHome     = "Home";

        #region Command handlers
        void cmdGoHome(VPServices app, Avatar who, bool entering)
        {
            AvatarPosition target;
            var query  = from   h in connection.Table<sqlHome>()
                         where  h.UserID == who.Id
                         select h;
            var home   = query.FirstOrDefault();

            if (home == null && entering)
                return;

            if (home != null)
                target = new AvatarPosition(home.X, home.Y, home.Z, home.Pitch, home.Yaw);
            else
                target = AvatarPosition.GroundZero;

            app.Bot.Avatars.Teleport(who.Session, "", target);
            Log.Debug(Name, "Teleported {0} home at {1:f3}, {2:f3}, {3:f3}", who.Name, target.X, target.Y, target.Z);
        }

        bool cmdSetHome(VPServices app, Avatar who, string data)
        {
            lock (app.DataMutex)
                connection.InsertOrReplace( new sqlHome
                {
                    UserID = who.Id,
                    X      = who.X,
                    Y      = who.Y,
                    Z      = who.Z,
                    Pitch  = who.Pitch,
                    Yaw    = who.Yaw
                });

            app.Notify(who.Session, "Set your home to {0:f3}, {1:f3}, {2:f3}" , who.X, who.Y, who.Z);
            return Log.Info(Name, "Set home for {0} at {1:f3}, {2:f3}, {3:f3}", who.Name, who.X, who.Y, who.Z);
        }

        bool cmdClearHome(VPServices app, Avatar who, string data)
        {
            lock (app.DataMutex)
                connection.Execute("DELETE FROM Home WHERE UserID = ?", who.Id);

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
            if ( VPServices.App.LastConnect.SecondsToNow() < 10 )
                return;

            var lastExit = who.GetSettingDateTime(settingLastExit);

            // Ignore bouncing/disconnected users
            if ( lastExit.SecondsToNow() < 10 )
                return;

            // Do not teleport home if bouncing
            if ( who.GetSetting(settingBounce) != null )
                who.DeleteSetting(settingBounce);
            else
                cmdGoHome(VPServices.App, who, true);
        }

        void onLeave(Instance sender, Avatar who)
        {
            // Keep track of LastExit to prevent annoying users
            who.SetSetting(settingLastExit, DateTime.Now);
        }
        #endregion
    }

    [Table("Home")]
    class sqlHome
    {
        [PrimaryKey, Unique]
        public int   UserID { get; set; }
        public float X      { get; set; }
        public float Y      { get; set; }
        public float Z      { get; set; }
        public float Pitch  { get; set; }
        public float Yaw    { get; set; }
    }
}
