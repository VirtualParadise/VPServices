using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices.Services
{
    public partial class Jumps : IService
    {
        public string Name
        { 
            get { return "Jumps"; }
        }

        public void Load (VPServices app, World bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Jumps: Add", "^(addjump|aj)$", cmdAddJump,
                    @"Adds a jump of the specified name at your position",
                    @"!aj `name`"
                ),

                new Command
                (
                    "Jumps: Delete", "^(deljump|dj)$", cmdDelJump,
                    @"Deletes a jump of the specified name",
                    @"!dj `name`"
                ),

                new Command
                (
                    "Jumps: List", "^(listjumps?|lj|jumps?list)$", cmdJumpList,
                    @"Searches list of jumps for name or creator matching given term",
                    @"!lj `search term`"
                ),

                new Command
                (
                    "Jumps: Jump", "^j(ump)?$", cmdJump,
                    @"Teleports you to the specified or a random jump",
                    @"!j `name|random`"
                ),
            });

            this.connection = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgAdded       = "Added jump '{0}' at {1}, {2}, {3} ({4} yaw, {5} pitch)";
        const string msgDeleted     = "Deleted jump '{0}'";
        const string msgResults     = "*** Search results for '{0}'";
        const string msgResult      = "!j {0}";
        const string msgResult2     = "➜ {0}X {1}Y {2}Z, {3} pitch {4} yaw";
        const string msgResult3     = "➜ by {0} on {1}";
        const string errExists      = "That jump already exists";
        const string errNonExistant = "That jump does not exist; try searching with !lj";
        const string errReserved    = "That name is reserved";
        const string errNotFound    = "Could not match any jumps or creators for '{0}'";

        SQLiteConnection connection; 
        #endregion

        #region Command handlers
        bool cmdAddJump(VPServices app, Avatar who, string data)
        {
            if (data == "")
                return false;
            else
                data = data.ToLower();

            if (data == "random")
                app.Warn(who.Session, errReserved);
            else
            {

                if (getJump(data) != null)
                {
                    app.Warn(who.Session, errExists);
                    Log.Debug(Name, "User '{0}' tried to add existing jump '{1}'", who.Name, data);
                    return true;
                }

                lock (app.DataMutex)
                    connection.Insert( new sqlJump
                    {
                        Name    = data,
                        Creator = who.Name,
                        When    = DateTime.Now,
                        X       = who.X,
                        Y       = who.Y,
                        Z       = who.Z,
                        Pitch   = who.Pitch,
                        Yaw     = who.Yaw
                    });

                app.NotifyAll(msgAdded, data, who.X, who.Y, who.Z, who.Yaw, who.Pitch);
                Log.Info(Name, "Saved a jump for user '{0}' at {1}, {2}, {3} named '{4}'", who.Name, who.X, who.Y, who.Z, data);
            }

            return true;
        }

        bool cmdDelJump(VPServices app, Avatar who, string data)
        {
            if (data == "")
                return false;
            else
                data = data.ToLower();
            
            if (data == "random")
                app.Warn(who.Session, errReserved);
            else
            {
                var jump = getJump(data);

                if (jump == null)
                {
                    app.Warn(who.Session, errNonExistant);
                    Log.Debug(Name, "User '{0}' tried to delete non-existant jump '{1}'", who.Name, data); 
                }
                else
                {
                    lock (app.DataMutex)
                        connection.Delete(jump);

                    app.NotifyAll(msgDeleted, data);
                    Log.Info(Name, "Deleted jump '{0}' for user '{1}'", data, who.Name);
                }
            }

            return true;
        }

        bool cmdJumpList(VPServices app, Avatar who, string data)
        {
            if (data == "")
                return false;

            lock (app.DataMutex)
            {
                var query = from    j in connection.Table<sqlJump>()
                            where  (j.Name + j.Creator).Contains(data)
                            select  j;

                if (query.Count() == 0)
                    app.Warn(who.Session, errNotFound, data);
                else
                {
                    app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgResults, data);

                    foreach ( var q in query )
                    {
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult , q.Name);
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult2, q.X, q.Y, q.Z, q.Pitch, q.Yaw);
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult3, q.Creator, q.When);
                    }
                }
            }

            return true;
        }

        bool cmdJump(VPServices app, Avatar who, string data)
        {
            if (data == "")
                return false;
            else
                data = data.ToLower();

            lock (app.DataMutex)
            {
                var jump = (data == "random")
                    ? connection.Query<sqlJump>("SELECT * FROM Jumps ORDER BY RANDOM() LIMIT 1;").FirstOrDefault()
                    : getJump(data);

                if (jump != null)
                    app.Bot.Avatars.Teleport(who.Session, "", new Vector3(jump.X, jump.Y, jump.Z), jump.Yaw, jump.Pitch);
                else
                    app.Warn(who.Session, errNonExistant); 
            }

            return true;
        } 
        #endregion

        #region Jump data logic
        sqlJump getJump(string name)
        {
            lock (VPServices.App.DataMutex)
            {
                var query = connection.Query<sqlJump>("SELECT * FROM Jumps WHERE Name = ? COLLATE NOCASE", name);

                return query.FirstOrDefault();
            }
        }
        #endregion
    }

    [Table("Jumps")]
    class sqlJump
    {
        [PrimaryKey, AutoIncrement]
        public int      ID      { get; set; }
        [Indexed]
        public string   Name    { get; set; }
        public string   Creator { get; set; }
        public float    X       { get; set; }
        public float    Y       { get; set; }
        public float    Z       { get; set; }
        public float    Yaw     { get; set; }
        public float    Pitch   { get; set; }
        public DateTime When    { get; set; }
    }
}
