using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VpNet.Core;
using VpNet.Core.EventData;
using VpNet.Core.Structs;

namespace VPServices.Services
{
    struct Jump
    {
        public string Name;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
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
            };
        }

        public string Save()
        {
            return string.Join(",",
                Name,
                X, Y, Z, Yaw);
        }
    }

    struct SpawnedJump
    {
        public int Id;
        public DateTime When;
    }

    class Jumps
    {
        public const string JUMPS = "Jumps.dat";
        public const string JUMP_ID = "!!!svcjump";
        public const string JUMP_DESC = "Click to teleport to {0}! (disappears after 30 seconds, say !back for a jump back)";
        public const string JUMPBACK_DESC = "Click to teleport back! (disappears after 30 seconds)";
        public const string JUMP_ACTION = "create color b, scale 2; activate teleportxyz {0} {1} {2} {3}; " + JUMP_ID;

        public const string JUMPLIST_DESC = "This list will disappear after 30 seconds:\n{0}";
        public const string JUMPLIST_ACTION = "create sign bcolor=b color=orange, scale 2;" + JUMP_ID;

        List<Jump> storedJumps = new List<Jump>();
        List<SpawnedJump> spawnedJumps = new List<SpawnedJump>();

        public Jumps()
        {
            // Load all saved telegrams
            if (File.Exists(JUMPS))
                foreach (var jump in File.ReadAllLines(JUMPS))
                    storedJumps.Add(Jump.FromString(jump));
        }

        public void Update()
        {
            try
            {
                foreach (var spawned in spawnedJumps)
                    if (DateTime.Now.Subtract(spawned.When).TotalSeconds > 30)
                    {
                        Console.WriteLine("Jumps: Deleting spawned jump, {0}", spawned.Id);
                        VPServices.Bot.DeleteObject(new VpObject { Id = spawned.Id });
                        spawnedJumps.Remove(spawned);
                    }
            }
            catch { }
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
                bot.Say(string.Format("{0}: Jump already exists", who.Name));
                return;
            }

            var x = (float)Math.Round(who.X, 2);
            var y = (float)Math.Round(who.Y, 2);
            var z = (float)Math.Round(who.Z, 2);
            storedJumps.Add(new Jump { Name = name, X = x, Y = y, Z = z, Yaw = who.Yaw });
            bot.Say(string.Format("{0}: Saved a jump at {1}, {2}, {3} for {4}",
                who.Name,
                who.X, who.Y, who.Z,
                name));

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
                bot.Say(string.Format("{0}: Jump does not exist", who.Name));
                return;
            }
            else storedJumps.Remove(jump);
            bot.Say(string.Format("{0}: Jump deleted", who.Name));
            Console.WriteLine("Deleted {0} jump for {1}", name, who.Name);
            SaveJumps();
        }

        public void CmdSpawnJump(Instance bot, Avatar who, string data)
        {
            var name = data.Trim().ToLower();
            if (name == "") return;

            var jump = (name == "random")
                //? storedJumps[new Random().Next(0, storedJumps.Count)]
                ? getJump(name)
                : getJump(name);
            if (jump.Name == name)
            {
                bot.CallbackObjectAdd += OnJumpAddCallback;
                bot.AddObject(new VpObject
                {
                    Model = "zcomp1.rwx",
                    Description = string.Format(JUMP_DESC, jump.Name),
                    Action = string.Format(JUMP_ACTION, jump.X, jump.Y, jump.Z, jump.Yaw),
                    Position = new Vector3(who.X, who.Y + 0.025f, who.Z)
                });
            }
            else
                bot.Say(string.Format("{0}: No such jump", who.Name));

            return;
        }

        public void CmdListJump(Instance bot, Avatar who, string data)
        {
            var list = string.Join(",\n",
                from j in storedJumps
                select j.Name);

            bot.CallbackObjectAdd += OnJumpAddCallback;
            bot.AddObject(new VpObject
            {
                Model = "button_4.rwx",
                Description = string.Format(JUMPLIST_DESC, list),
                Action = string.Format(JUMPLIST_ACTION),
                Position = new Vector3(who.X, who.Y + 0.025f, who.Z)
            });

            return;
        }

        public void OnJumpAddCallback(Instance bot, int id)
        {
            Console.WriteLine("Jumps: Spawned jump monitored, {0}", id);
            spawnedJumps.Add(new SpawnedJump { Id = id, When = DateTime.Now });
            bot.CallbackObjectAdd -= OnJumpAddCallback;
        }

        Jump getJump(string name)
        {
            foreach (var jump in storedJumps)
                if (jump.Name == name) return jump;

            return Jump.Empty;
        }
    }
}
