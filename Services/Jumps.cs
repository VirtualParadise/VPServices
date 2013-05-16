using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VP;

namespace VPServices.Services
{
    public class Jumps : IService
    {
        const string msgAdded       = "Added jump '{0}' at {1}, {2}, {3} ({4} yaw, {5} pitch)";
        const string msgDeleted     = "Deleted jump '{0}'";
        const string msgExists      = "That jump already exists";
        const string msgNonExistant = "That jump does not exist; check {0}";
        const string msgReserved    = "That name is reserved";
        const string msgResults     = "*** Search results for '{0}'";
        const string msgResult      = "!j {0}";
        const string msgNoResults   = "No results; check {0}";
        const string fileJumps      = "Jumps.dat";
        const string webJumps       = "jumps";

        List<Jump>   storedJumps = new List<Jump>();

        public string Name { get { return "Jumps"; } }
        public void   Init (VPServices app, Instance bot)
        {
            // Load all saved jumps
            if (  File.Exists(fileJumps) )
                foreach ( var jump in File.ReadAllLines(fileJumps) )
                    storedJumps.Add( new Jump(jump) );

            app.Commands.AddRange(new[] {
                new Command
                (
                    "Jumps: Add", "^(addjump|aj)$", cmdAddJump,
                    @"Adds a jump of the specified name at user's position",
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
                    @"Prints the URL to a listing of jumps to chat or lists those matching a search term",
                    @"!lj `[search]`"
                ),

                new Command
                (
                    "Jumps: Jump", "^j(ump)?$", cmdJump,
                    @"Teleports user to the specified or a random jump",
                    @"!j `name|random`"
                ),
            });

            app.Routes.Add(new WebRoute("Jumps", "^(list)jumps?$", webListJumps,
                @"Provides a list of jump points registered in the system"));
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose()
        {
            saveJumps();
            storedJumps.Clear();
        }

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

            if ( getJump(name).Name != "" )
            {
                app.Warn(who.Session, msgExists);
                return Log.Debug(Name, "{0} tried to overwrite jump {1}", who.Name, getJump(name).Name);
            }

            var x = (float) Math.Round(who.X, 3);
            var y = (float) Math.Round(who.Y, 3);
            var z = (float) Math.Round(who.Z, 3);
            storedJumps.Add(new Jump { Name = name, X = x, Y = y, Z = z, Yaw = who.Yaw, Pitch = who.Pitch });
            saveJumps();

            app.NotifyAll(msgAdded, name, x, y, z, who.Yaw, who.Pitch);
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
            if ( jump.Name == "" )
            {
                app.Warn(who.Session, msgNonExistant, jumpsUrl);
                return Log.Debug(Name, "{1} tried to delete non-existant jump {0}", name, who.Name);
            }
            else
                storedJumps.Remove(jump);

            saveJumps();
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

            var query = from j in storedJumps
                        where j.Name.Contains(data)
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
                app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult, q.Name);

            return true;
        }

        bool cmdJump(VPServices app, Avatar who, string data)
        {
            var jumpsUrl = app.PublicUrl + webJumps;
            var name     = data.ToLower();

            // Reject null
            if ( name == "" )
                return false;

            var rand = new Random().Next(0, storedJumps.Count);
            var jump = ( name == "random" )
                ? storedJumps[rand]
                : getJump(name);

            if ( jump.Name == name || name == "random" )
                app.Bot.Avatars.Teleport(who.Session, "", new Vector3(jump.X, jump.Y, jump.Z), jump.Yaw, jump.Pitch);
            else
                app.Warn(who.Session, msgNonExistant, jumpsUrl);

            return true;
        } 
        #endregion

        #region Web routes
        string webListJumps(VPServices serv, string data)
        {
            string listing = "# Jump points available:\n";
            storedJumps.Sort();

            foreach ( var jump in storedJumps )
            {
                listing += string.Format(
@"## !jump {0}

* **Coordinates:** {1:f3}, {2:f3}, {3:f3}
* **Pitch / yaw:** {4:f0} / {5:f0}

", jump.Name, jump.X, jump.Y, jump.Z, jump.Pitch, jump.Yaw);
            }

            return serv.MarkdownParser.Transform(listing);
        } 
        #endregion

        #region Jump data logic
        Jump getJump(string name)
        {
            foreach ( var jump in storedJumps )
                if ( jump.Name == name ) return jump;

            return Jump.Empty;
        }

        void saveJumps()
        {
            File.WriteAllLines(fileJumps,
                from t in storedJumps
                select t.ToString(), Encoding.UTF8);
        } 
        #endregion
    }

    struct Jump : IComparable<Jump>
    {
        public string Name;
        public float  X;
        public float  Y;
        public float  Z;
        public float  Yaw;
        public float  Pitch;
        public static Jump Empty = new Jump { Name = "" };

        /// <summary>
        /// Creates a jump from a CSV string
        /// </summary>
        public Jump(string csv)
        {
            if (csv == "" || !csv.Contains(","))
                throw new ArgumentNullException();

            var parts = csv.Split(new[] { "," }, StringSplitOptions.None);
            Name      = parts[0];
			X         = float.Parse(parts[1], CultureInfo.InvariantCulture);
			Y         = float.Parse(parts[2], CultureInfo.InvariantCulture);
			Z         = float.Parse(parts[3], CultureInfo.InvariantCulture);
			Yaw       = float.Parse(parts[4], CultureInfo.InvariantCulture);
			Pitch     = float.Parse(parts[5], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats the jump to a CSV string
        /// </summary>
        public override string ToString()
        {
			return string.Format(CultureInfo.InvariantCulture, 
			                     "{0},{1},{2},{3},{4},{5}", 
			                     Name, X, Y, Z, Yaw, Pitch);
        }

        public int CompareTo(Jump other) { return this.Name.CompareTo(other.Name); }
    }
}
