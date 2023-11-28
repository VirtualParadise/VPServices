﻿using System;
using System.Collections.Generic;
using VpNet;
using SQLite;
using Serilog;
using VPServices.Extensions;

namespace VPServices.Services
{
    /// <summary>
    /// Handles home setting / teleport and bouncing
    /// </summary>
    public partial class Home : IService
    {
        readonly ILogger logger = Log.ForContext("Tag", nameof(Home));
        public string Name
        { 
            get { return "Home"; }
        }

        public void Init(VPServices app, VirtualParadiseClient bot)
        {
            app.Commands.Add(new Command(
                "Home: Set", "^sethome$", cmdSetHome,
                @"Sets user's home position, where they will be teleported to every time they enter the world",
                @"!sethome"
            ));

            app.Commands.Add(new Command(
                "Home: Teleport", "^home$",
                (s,a,d) => { cmdGoHome(s, a, false); return true; },
                @"Teleports user to their home position, or ground zero if unset",
                @"!home"
            ));

            app.Commands.Add(new Command(
                "Home: Clear", "^clearhome$", cmdClearHome,
                @"Clears user's home position",
                @"!clearhome"
            ));

            app.Commands.Add(new Command(
                "Teleport: Bounce", "^bounce$", cmdBounce,
                @"Disconnects and reconnects user to the world; useful for clearing the download queue and fixing some issues",
                @"!bounce"
            ));

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
            var query  = from   h in connection.Table<sqlHome>()
                         where  h.UserID == who.User.Id
                         select h;
            var home   = query.FirstOrDefault();

            if (home == null && entering)
                return;

            if (home != null)
            {
                // Note: Home DB and VP SDK define yaw and pitch on different axes -- to maintain backwards compatibility with old home,
                // continue switching Yaw/Pitch axes to home DB and just switch them back in code. Will look into fixing DB later.
                app.Bot.Teleport(who, new Location("", new Vector3(home.X, home.Y, home.Z), new Vector3(home.Pitch, home.Yaw, 0)));
                logger.Debug("Teleported {User} home at {X:f3}, {Y:f3}, {Z:f3}", who.Name, home.X, home.Y, home.Z);
            }
            else
            {
                app.Bot.Teleport(who, new Location("", new Vector3(), new Vector3()));
                logger.Debug("Teleported {0} home (to ground zero) at {X:f3}, {Y:f3}, {Z:f3}", who.Name, 0, 0, 0);
            }
        }

        bool cmdSetHome(VPServices app, Avatar who, string data)
        {
            lock (app.DataMutex)
                // Note: Home DB and VP SDK define yaw and pitch on different axes -- to maintain backwards compatibility with old home,
                // continue switching Yaw/Pitch axes to home DB and just switch them back in code. Will look into fixing DB later.
                connection.InsertOrReplace( new sqlHome
                {
                    UserID = who.User.Id,
                    X      = (float)who.Location.Position.X,
                    Y      = (float)who.Location.Position.Y,
                    Z      = (float)who.Location.Position.Z,
                    Pitch  = (float)who.Location.Rotation.X,
                    Yaw    = (float)who.Location.Rotation.Y,
                });

            app.Notify(who.Session, "Set your home to {0:f3}, {1:f3}, {2:f3}" , who.Location.Position.X, who.Location.Position.Y, who.Location.Position.Z);
            logger.Information("Set home for {User} at {X:f3}, {Y:f3}, {Z:f3}", who.Name, who.Location.Position.X, who.Location.Position.Y, who.Location.Position.Z);
            return true;
        }

        bool cmdClearHome(VPServices app, Avatar who, string data)
        {
            lock (app.DataMutex)
                connection.Execute("DELETE FROM Home WHERE UserID = ?", who.User.Id);

            app.Notify(who.Session, "Your home has been cleared to ground zero");
            logger.Information("Cleared home for {User}", who.Name);
            return true;
        }

        bool cmdBounce(VPServices app, Avatar who, string data)
        {
            who.SetSetting(settingBounce, true);
            app.Bot.Teleport(who, new Location(app.World, who.Location.Position, who.Location.Rotation));

            logger.Information("Bounced user {0}", who.Name);
            return true;
        } 
        #endregion

        #region Event handlers
        void onEnter(VirtualParadiseClient sender, Avatar who)
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

        void onLeave(VirtualParadiseClient sender, Avatar who)
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
