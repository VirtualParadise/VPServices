using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VP;

namespace VPServ.Services
{
    public class Jumps : IService
    {
        public const string FILE_JUMPS = "Jumps.dat";

        List<Jump> storedJumps = new List<Jump>();

        public string Name { get { return "Jumps"; } }
        public void Init(VPServ app, Instance bot)
        {
            // Load all saved jumps
            if (File.Exists(FILE_JUMPS))
                foreach (var jump in File.ReadAllLines(FILE_JUMPS))
                    storedJumps.Add(new Jump(jump));

            app.Commands.AddRange(new[] {
                new Command("Add jump", "^(addjump|aj)$", cmdAddJump,
                @"Adds a jump of the specified name at the requester's position in the format: `!addjump *name*`"),

                new Command("Delete jump", "^(deljump|dj)$", cmdDelJump,
                @"Deletes a jump of the specified name in the format: `!deljump *name*`"),

                new Command("List jumps", "^(listjumps?|lj|jumps?list)$", (s,a,d) => { s.Bot.Say(s.PublicUrl + "jumps"); },
                @"Prints the URL to a listing of jumps to chat", 60),

                new Command("Jump", "^j(ump)?$", cmdJump,
                @"Teleports the requester to the specified jump in the format: `!jump *name*`"),
            });

            app.Routes.Add(new WebRoute("Jumps", "^(list)jumps?$", webListJumps,
                @"Provides a list of jump points registered in the system"));
        }

        public void Dispose()
        {
            saveJumps();
            storedJumps.Clear();
        }

        void saveJumps()
        {
            File.WriteAllLines(FILE_JUMPS,
                from t in storedJumps
                select t.ToString(), Encoding.UTF8);
        }

        void cmdAddJump(VPServ serv, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "" || name == "random") return;

            if (getJump(name).Name != "")
            {
                serv.Bot.Say("{0}: Jump already exists", who.Name);
                Log.Debug(Name, "{0} tried to overwrite jump {1}", who.Name, getJump(name).Name);
                return;
            }

            var x = (float)Math.Round(who.X, 3);
            var y = (float)Math.Round(who.Y, 3);
            var z = (float)Math.Round(who.Z, 3);
            storedJumps.Add(new Jump { Name = name, X = x, Y = y, Z = z, Yaw = who.Yaw, Pitch = who.Pitch });
            saveJumps();

            serv.Bot.Say("{0}: Saved a jump at {1}, {2}, {3} for {4}", who.Name, x, y, z, name);
            Log.Info(Name, "Saved a jump for {0} at {1}, {2}, {3} for {4}", who.Name, who.X, who.Y, who.Z, name);
        }

        void cmdDelJump(VPServ serv, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "" || name == "random") return;

            var jump = getJump(name);
            if (jump.Name == "")
            {
                serv.Bot.Say("{0}: Jump does not exist", who.Name);
                Log.Debug(Name, "{1} tried to delete non-existant jump {0}", name, who.Name);
                return;
            }
            else storedJumps.Remove(jump);
            saveJumps();

            serv.Bot.Say("{0}: Jump deleted", who.Name);
            Log.Info(Name, "Deleted {0} jump for {1}", name, who.Name);
        }

        void cmdJump(VPServ serv, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "") return;

            var jump = (name == "random")
                ? storedJumps[new Random().Next(0, storedJumps.Count)]
                : getJump(name);
            if (jump.Name == name || name == "random")
                serv.Bot.Avatars.Teleport(who.Session, "", new Vector3(jump.X, jump.Y, jump.Z), jump.Yaw, jump.Pitch);
            else
                serv.Bot.Say("{0}: No such jump", who.Name);
        }

        Jump getJump(string name)
        {
            foreach (var jump in storedJumps)
                if (jump.Name == name) return jump;

            return Jump.Empty;
        }

        string webListJumps(VPServ serv, string data)
        {
            string listing = "# Jump points available:\n";
            storedJumps.Sort();

            foreach (var jump in storedJumps)
            {
                listing += string.Format(
@"## !jump {0}

* **Coordinates:** {1:f3}, {2:f3}, {3:f3}
* **Pitch / yaw:** {4:f0} / {5:f0}

", jump.Name, jump.X, jump.Y, jump.Z, jump.Pitch, jump.Yaw);
            }

            return serv.MarkdownParser.Transform(listing);
        }
    }

    struct Jump : IComparable<Jump>
    {
        public string Name;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
        public float Pitch;
        public static Jump Empty = new Jump { Name = "" };

        /// <summary>
        /// Creates a jump from a CSV string
        /// </summary>
        public Jump(string csv)
        {
            if (csv == "" || !csv.Contains(","))
                throw new ArgumentNullException();

            var parts = csv.Split(new[] { "," }, StringSplitOptions.None);
            Name = parts[0];
            X = float.Parse(parts[1]);
            Y = float.Parse(parts[2]);
            Z = float.Parse(parts[3]);
            Yaw = float.Parse(parts[4]);
            Pitch = float.Parse(parts[5]);
        }

        /// <summary>
        /// Formats the jump to a CSV string
        /// </summary>
        public override string ToString()
        {
            return string.Join(",", Name, X, Y, Z, Yaw, Pitch);
        }

        public int CompareTo(Jump other) { return this.Name.CompareTo(other.Name); }
    }
}
