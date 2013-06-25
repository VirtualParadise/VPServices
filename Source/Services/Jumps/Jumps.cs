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

        public void Init (VPServices app, Instance bot)
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
                    @"Prints the URL to a listing of jumps to chat or lists those matching a search term to you",
                    @"!lj `[search]`"
                ),

                new Command
                (
                    "Jumps: Jump", "^j(ump)?$", cmdJump,
                    @"Teleports you to the specified or a random jump",
                    @"!j `name|random`"
                ),
            });

            app.Routes.Add(new WebRoute("Jumps", "^(list)?jumps?$", webListJumps,
                @"Provides a list of jump points registered in the system"));

            this.connection = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgAdded       = "Added jump '{0}' at {1}, {2}, {3} ({4} yaw, {5} pitch)";
        const string msgDeleted     = "Deleted jump '{0}'";
        const string msgExists      = "That jump already exists";
        const string msgNonExistant = "That jump does not exist; check {0}";
        const string msgReserved    = "That name is reserved";
        const string msgResults     = "*** Search results for '{0}'";
        const string msgResult      = "!j {0}   - by {1} on {2}";
        const string msgNoResults   = "No results; check {0}";
        const string webJumps       = "jumps";

        SQLiteConnection connection; 
        #endregion

        #region Command handlers
        bool cmdAddJump(VPServices app, Avatar who, string data)
        {
            var name = data.ToLower();

            // Reject null entries and reserved words
            if ( name == "" )
                return false;
            else if ( name == "random" )
            {
                app.Warn(who.Session, msgReserved);
                return true;
            }

            if ( getJump(name) != null )
            {
                app.Warn(who.Session, msgExists);
                return Log.Debug(Name, "{0} tried to overwrite jump {1}", who.Name, getJump(name).Name);
            }

            connection.Insert( new sqlJump
            {
                Name    = name,
                Creator = who.Name,
                When    = DateTime.Now,
                X       = who.X,
                Y       = who.Y,
                Z       = who.Z,
                Pitch   = who.Pitch,
                Yaw     = who.Yaw
            });

            app.NotifyAll(msgAdded, name, who.X, who.Y, who.Z, who.Yaw, who.Pitch);
            return Log.Info(Name, "Saved a jump for {0} at {1}, {2}, {3} for {4}", who.Name, who.X, who.Y, who.Z, name);
        }

        bool cmdDelJump(VPServices app, Avatar who, string data)
        {
            var jumpsUrl = app.PublicUrl + webJumps;
            var name     = data.ToLower();

            // Reject null entries and reserved words
            if ( name == "" )
                return false;
            else if ( name == "random" )
            {
                app.Warn(who.Session, msgReserved);
                return true;
            }

            var jump = getJump(name);
            if ( jump == null )
            {
                app.Warn(who.Session, msgNonExistant, jumpsUrl);
                return Log.Debug(Name, "{1} tried to delete non-existant jump {0}", name, who.Name);
            }
            else
                connection.Delete(jump);

            app.NotifyAll(msgDeleted, name);
            return Log.Info(Name, "Deleted {0} jump for {1}", name, who.Name);
        }

        bool cmdJumpList(VPServices app, Avatar who, string data)
        {
            var jumpsUrl = app.PublicUrl + webJumps;

            // No search; list URL only
            if ( data == "" )
            {
                app.Notify(who.Session, jumpsUrl);
                return true;
            }

            var query = from   j in connection.Table<sqlJump>()
                        where  j.Name.Contains(data)
                        select j;

            // No results
            if ( query.Count() <= 0 )
            {
                app.Warn(who.Session, msgNoResults, jumpsUrl);
                return true;
            }

            // Iterate results
            app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgResults, data);
            foreach ( var q in query )
                app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult, q.Name, q.Creator, q.When);

            return true;
        }

        bool cmdJump(VPServices app, Avatar who, string data)
        {
            var jumpsUrl = app.PublicUrl + webJumps;
            var name     = data.ToLower();

            // Reject null
            if ( name == "" )
                return false;

            var jump  = ( name == "random" )
                ? connection.Query<sqlJump>("SELECT * FROM Jumps ORDER BY RANDOM() LIMIT 1;").FirstOrDefault()
                : getJump(name);

            if ( jump != null )
                app.Bot.Avatars.Teleport(who.Session, "", new Vector3(jump.X, jump.Y, jump.Z), jump.Yaw, jump.Pitch);
            else
                app.Warn(who.Session, msgNonExistant, jumpsUrl);

            return true;
        } 
        #endregion

        #region Web routes
        string webListJumps(VPServices serv, string data)
        {
            var listing = "# Jump points available:\n";
            var list    = connection.Query<sqlJump>("SELECT * FROM Jumps ORDER BY Name ASC;");

            foreach ( var jump in list )
            {
                listing +=
@"## !jump {0}

* **Coordinates:** {1:f3}, {2:f3}, {3:f3}
* **Pitch / yaw:** {4:f0} / {5:f0}
* **Creator:** {6}
* **Created:** {7}

".LFormat(jump.Name, jump.X, jump.Y, jump.Z, jump.Pitch, jump.Yaw, jump.Creator, jump.When);
            }

            return serv.MarkdownParser.Transform(listing);
        } 
        #endregion

        #region Jump data logic
        sqlJump getJump(string name)
        {
            var query = connection.Query<sqlJump>("SELECT * FROM Jumps WHERE Name = ? COLLATE NOCASE", name);

            return query.FirstOrDefault();
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
