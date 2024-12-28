using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using VpNet;
using VPServices.Extensions;

namespace VPServices.Services
{
    public partial class Jumps : IService
    {
        public string Name
        { 
            get { return "Jumps"; }
        }
        readonly ILogger logger = Log.ForContext("Tag", "Jumps");

        public void Init (VPServices app, VirtualParadiseClient bot)
        {
            app.Commands.Add(new Command(
                "Jumps: Add", "^(addjump|aj)$", cmdAddJump,
                @"Adds a jump of the specified name at your position",
                @"!aj `name`"
            ));

            app.Commands.Add(new Command(
                "Jumps: Delete", "^(deljump|dj)$", cmdDelJump,
                @"Deletes a jump of the specified name",
                @"!dj `name`"
            ));

            app.Commands.Add(new Command(
                "Jumps: List", "^(listjumps?|lj|jumps?list)$", cmdJumpList,
                @"Prints the URL to a listing of jumps to chat or lists those matching a search term to you",
                @"!lj `[search]`"
            ));

            app.Commands.Add(new Command(
                "Jumps: Jump", "^j(ump)?$", cmdJump,
                @"Teleports you to the specified or a random jump",
                @"!j `name|random`"
            ));

            app.Routes.Add(new WebRoute("Jumps", "^(list)?jumps?$", webListJumps,
                @"Provides a list of jump points registered in the system"));

            this.connection = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgAdded       = "Added jump '{0}' at {1}, {2}, {3} ({4} yaw, {5} pitch)";
        const string msgDeleted     = "Deleted jump '{0}'";
        const string msgNotAllowed  = "That action is not allowed for this jump";
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
                logger.Debug("{User} tried to overwrite jump {Jump}", who.Name, getJump(name).Name);
                return true;
            }

            lock (app.DataMutex)
                // Note: Jump DB and VP SDK define yaw and pitch on different axes -- to maintain backwards compatibility with old jumps,
                // continue switching Yaw/Pitch axes to jump DB and just switch them back in code. Will look into fixing DB later.
                connection.Insert( new sqlJump
                {
                    Name    = name,
                    Creator = who.Name,
                    When    = DateTime.Now,
                    X       = (float)who.Location.Position.X,
                    Y       = (float)who.Location.Position.Y,
                    Z       = (float)who.Location.Position.Z,
                    Pitch   = (float)who.Location.Rotation.X,
                    Yaw     = (float)who.Location.Rotation.Y
                });

            var compass = CompassExtensions.ToCompassTuple(who);

            app.NotifyAll(msgAdded, name, who.Location.Position.X, who.Location.Position.Y, who.Location.Position.Z, compass.Angle, who.Location.Rotation.X);
            logger.Information("Saved a jump for {User} at {Position} for {Jump}", who.Name, who.Location.Position, name);
            return true;
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
                logger.Debug("{User} tried to delete non-existant jump {Jump}", name, who.Name);
                return true;
			}
			else if (
				!who.Name.Equals(jump.Creator, StringComparison.OrdinalIgnoreCase) &&
				!app.IsOwner(who.Name))
			{
				app.Warn(who.Session, msgNotAllowed);
				logger.Warning("{User} tried to delete jump created by {Creator}", who.Name, jump.Creator);
                return true;
			}
			else
                lock (app.DataMutex)
                    connection.Delete(jump);

            app.NotifyAll(msgDeleted, name);
            logger.Information("deleted {Jump} jump for {User}", name, who.Name);
            return true;
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

            lock ( app.DataMutex )
            {
                var query = from j in connection.Table<sqlJump>()
                            where j.Name.Contains(data)
                            select j;

                // No results
                if ( query.Count() <= 0 )
                {
                    app.Warn(who.Session, msgNoResults, jumpsUrl);
                    return true;
                }

                // Iterate results
                app.Bot.ConsoleMessage(who.Session, "", string.Format(msgResults, data), VPServices.ColorInfo, TextEffectTypes.BoldItalic);
                foreach ( var q in query )
                    app.Bot.ConsoleMessage(who.Session, "", string.Format( msgResult, q.Name, q.Creator, q.When), VPServices.ColorInfo, TextEffectTypes.Italic);
            }

            return true;
        }

        bool cmdJump(VPServices app, Avatar who, string data)
        {
            var jumpsUrl = app.PublicUrl + webJumps;
            var name     = data.ToLower();

            // Reject null
            if ( name == "" )
                return false;

            lock ( app.DataMutex )
            {
                var jump  = ( name == "random" )
                        ? connection.Query<sqlJump>("SELECT * FROM Jumps ORDER BY RANDOM() LIMIT 1;").FirstOrDefault()
                        : getJump(name);

                if (jump != null)
                    // Note: Jump DB and VP SDK define yaw and pitch on different axes -- to maintain backwards compatibility with old jumps,
                    // continue switching Yaw/Pitch axes to jump DB and just switch them back in code. Will look into fixing DB later.
                    app.Bot.Teleport(who, new Location("", new Vector3(jump.X, jump.Y, jump.Z), new Vector3(jump.Pitch, jump.Yaw, 0)));
                else
                    app.Warn(who.Session, msgNonExistant, jumpsUrl); 
            }

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
$@"## !jump {jump.Name}

* **Coordinates:** {jump.X:f3}, {jump.Y:f3}, {jump.Z:f3}
* **Pitch / yaw:** {jump.Pitch:f0} / {jump.Yaw:f0}
* **Creator:** {jump.Creator}
* **Created:** {jump.When}

";
            }

            return serv.MarkdownParser.Transform(listing);
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
