using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    struct Jump
    {
        public string Name;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
        public float Pitch;
        public static Jump Empty = new Jump { Name = "" };

        public static Jump FromString(string dat)
        {
            if (dat == "" || !dat.Contains(","))
                throw new ArgumentNullException();

            var parts = dat.Split(new[] { "," }, StringSplitOptions.None);
            return new Jump
            {
                Name = parts[0],
                X = float.Parse(parts[1]),
                Y = float.Parse(parts[2]),
                Z = float.Parse(parts[3]),
                Yaw = float.Parse(parts[4]),
                Pitch = float.Parse(parts[5])
            };
        }

        public string Save()
        {
            return string.Join(",",
                Name,
                X, Y, Z,
                Yaw, Pitch);
        }
    }

    class Jumps
    {
        public const string JUMPS = "Jumps.dat";

        List<Jump> storedJumps = new List<Jump>();

        public Jumps()
        {
            // Load all saved telegrams
            if (File.Exists(JUMPS))
                foreach (var jump in File.ReadAllLines(JUMPS))
                    storedJumps.Add(Jump.FromString(jump));
        }

        public void SaveJumps()
        {
            File.WriteAllLines(JUMPS,
                from t in storedJumps
                select t.Save()
                , Encoding.UTF8);
        }

        public void CmdAddJump(Instance bot, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "" || name == "random") return;

            if (getJump(name).Name != "")
            {
                bot.Say("{0}: Jump already exists", who.Name);
                return;
            }

            var x = (float)Math.Round(who.X, 2);
            var y = (float)Math.Round(who.Y, 2);
            var z = (float)Math.Round(who.Z, 2);
            storedJumps.Add(new Jump { Name = name, X = x, Y = y, Z = z, Yaw = who.Yaw, Pitch = who.Pitch });
            bot.Say("{0}: Saved a jump at {1}, {2}, {3} for {4}",
                who.Name,
                who.X, who.Y, who.Z,
                name);

            Console.WriteLine("Saved a jump for {0} at {1}, {2}, {3} for {4}",
                who.Name,
                who.X, who.Y, who.Z,
                name);
            SaveJumps();
        }

        public void CmdDelJump(Instance bot, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "" || name == "random") return;

            var jump = getJump(name);
            if (jump.Name == "")
            {
                bot.Say("{0}: Jump does not exist", who.Name);
                return;
            }
            else storedJumps.Remove(jump);
            bot.Say("{0}: Jump deleted", who.Name);
            Console.WriteLine("Deleted {0} jump for {1}", name, who.Name);
            SaveJumps();
        }

        public void CmdJump(Instance bot, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "") return;

            var jump = (name == "random")
                ? storedJumps[new Random().Next(0, storedJumps.Count)]
                : getJump(name);
            if (jump.Name == name || name == "random")
                bot.Avatars.Teleport(
                    who.Session,
                    "",
                    new Vector3
                    {
                        X = jump.X,
                        Y = jump.Y,
                        Z = jump.Z
                    },
                    jump.Yaw, jump.Pitch);
            else
                bot.Say("{0}: No such jump", who.Name);

            return;
        }

        Jump getJump(string name)
        {
            foreach (var jump in storedJumps)
                if (jump.Name == name) return jump;

            return Jump.Empty;
        }
    }
}
