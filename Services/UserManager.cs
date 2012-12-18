using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;
using Nini.Config;

namespace VPServices.Services
{
    public class ServicesUser : IDisposable
    {
        public Stack<AvatarPosition> TeleportHistory = new Stack<AvatarPosition>();
        public AvatarPosition LastPosition;
        public Avatar Avatar;

        public IConfig Settings;

        public string Name { get { return Avatar.Name; } }
        public int Session { get { return Avatar.Session; } }
        public AvatarPosition Position
        {
            get
            {
                return new AvatarPosition
                {
                    X = Avatar.X,
                    Y = Avatar.Y,
                    Z = Avatar.Z,
                    Pitch = Avatar.Pitch,
                    Yaw = Avatar.Yaw
                };
            }
        }

        public void Dispose()
        {
            TeleportHistory.Clear();
            Settings = null;
        }
    }

    class UserManager : List<ServicesUser>, IDisposable
    {
        public const int TELEPORT_THRESHOLD = 20;
        public const string FILE_USERSETTINGS = "UserSettings.ini";
        public const string SETTING_HOME = "Home";

        /// <summary>
        /// User entry/exit monitor
        /// </summary>
        public static StreamWriter UserMon = new StreamWriter("UserHist.dat", true)
        {
            AutoFlush = true
        };

        /// <summary>
        /// User settings INI
        /// </summary>
        public IniConfigSource UserSettings = new IniConfigSource
        {
            AutoSave = true
        };

        public int UniqueUsers = 0;
        public int Bots = 0;

        public UserManager()
        {
            VPServices.Bot.Avatars.Enter += OnAvatarAdd;
            VPServices.Bot.Avatars.Leave += OnAvatarDelete;
            VPServices.Bot.Avatars.Change += OnAvatarChange;

            if (File.Exists(FILE_USERSETTINGS))
                UserSettings.Load(FILE_USERSETTINGS);
            else
                UserSettings.Save(FILE_USERSETTINGS);
        }

        /// <summary>
        /// Gets case-insensitive user by name or returns null
        /// </summary>
        public ServicesUser this[string name]
        {
            get
            {
                foreach (var user in this)
                    if (user.Name.ToLower() == name.ToLower())
                        return user;

                return null;
            }
        }

        /// <summary>
        /// Gets user by session number or returns null
        /// </summary>
        public new ServicesUser this[int session]
        {
            get
            {
                foreach (var user in this)
                    if (user.Session == session)
                        return user;

                return null;
            }
        }

        /// <summary>
        /// Checks if user is kickbanned, otherwise logs them in the entry log and
        /// creates a user entry
        /// </summary>
        public void OnAvatarAdd(Instance sender, Avatar avatar)
        {
            if (VPServices.KickBans.IsKickBanned(avatar.Name))
            {
                VPServices.KickBans.Eject(avatar.Session);
                return;
            }

            // Write to log
            Console.WriteLine("User {0} has entered", avatar.Name);
            UserMon.WriteLine("enter,{0},{1}",
                avatar.Name,
                (int) DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);

            var user = new ServicesUser { Avatar = avatar };
            user.LastPosition = user.Position;
            this.Add(user);

            // Do not load settings for bots
            if (avatar.IsBot)
                Bots++;
            else
            {
                // Only add to unique user counts if name is not present
                if (this[avatar.Name] != null) UniqueUsers++;

                // Load / create settings
                user.Settings = UserSettings.Configs[avatar.Name] ?? UserSettings.AddConfig(avatar.Name);

                // Teleport home
                if (DateTime.Now.Subtract(VPServices.StartUpTime).TotalSeconds > 10
                    && user.Settings.Contains(SETTING_HOME))
                    CmdGoHome(user.Avatar);
            }
        }

        /// <summary>
        /// Logs exits of unejected users
        /// </summary>
        public void OnAvatarDelete(Instance sender, Avatar avatar)
        {
            // Write to log
            Console.WriteLine("User {0} has exited", avatar.Name);
            UserMon.WriteLine("leave,{0},{1}",
                avatar.Name,
                (int) DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);

            var user = this[avatar.Session];
            this.Remove(user);

            if (avatar.IsBot)
                Bots--;
            else
            {
                if (this[avatar.Name] == null) UniqueUsers--;
                user.Dispose();
            }
        }

        /// <summary>
        /// Tracks user movements for teleport history
        /// </summary>
        public void OnAvatarChange(Instance sender, Avatar avatar)
        {
            var user = this[avatar.Session];
            if (user == null) return;

            user.Avatar = avatar;
            var ll = user.LastPosition;
            var nl = user.Position;
            
            if (Math.Abs(avatar.X - ll.X) > TELEPORT_THRESHOLD
                || Math.Abs(avatar.Y - ll.Y) > (TELEPORT_THRESHOLD * 2)
                || Math.Abs(avatar.Z - ll.Z) > TELEPORT_THRESHOLD)
            {
                Console.WriteLine("Teleport history recorded for {0}", avatar.Name);
                user.TeleportHistory.Push(ll);
            }
            
            user.LastPosition = nl;
        }

        /// <summary>
        /// Handles the !back command
        /// </summary>
        public void CmdGoBack(Instance bot, Avatar who, string data)
        {
            var user = this[who.Session];
            if (user == null) return;

            if (user.TeleportHistory.Count == 0)
            {
                bot.Say("{0}: No teleport history", who.Name);
                return;
            }

            var jump = user.TeleportHistory.Pop();
            bot.Avatars.Teleport(
                who.Session,
                "",
                new Vector3
                {
                    X = jump.X,
                    Y = jump.Y,
                    Z = jump.Z
                }, 0, 0);

            return;
        }

        public void CmdGoHome(Avatar user)
        {
            var pos = this[user.Name].Settings
                .Get(SETTING_HOME, "0,0,0,0,0");

            VPServices.Bot.Avatars.Teleport(
                user.Session,
                "",
                new AvatarPosition(pos));
        }

        public void CmdSetHome(Avatar av)
        {
            var user = this[av.Name];
            user.Settings.Set(SETTING_HOME, user.Position.ToString());
                
            VPServices.Bot.Say("{0}: Set your home to {1}, {2}, {3}",
                av.Name,
                av.X, av.Y, av.Z);

            Console.WriteLine("Set home for {0} at {1}, {2}, {3}",
                av.Name,
                av.X, av.Y, av.Z);
        }

        /// <summary>
        /// Closes the user entry/exit monitor and saves settings
        /// </summary>
        public void Dispose()
        {
            UserMon.Flush();
            UserMon.Close();
            UserSettings.Save();
        }

        
    }
}
