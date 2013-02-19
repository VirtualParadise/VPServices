using Nini.Config;
using System;
using System.Collections.Generic;
using System.IO;
using VP;

namespace VPServ
{
    public partial class VPServ
    {
        public const string FILE_USERSETTINGS = "UserSettings.ini";

        /// <summary>
        /// User settings INI
        /// </summary>
        public IniConfigSource UserSettings = new IniConfigSource { AutoSave = true };

        /// <summary>
        /// Global list of currently present users
        /// </summary>
        public List<Avatar> Users = new List<Avatar>();

        public int UniqueUsers = 0;
        public int Bots = 0;

        public void SetupUserSettings()
        {
            Bot.Avatars.Enter += onAvatarAdd;
            Bot.Avatars.Leave += onAvatarDelete;
            Bot.Avatars.Change += onAvatarsChange;

            if (File.Exists(FILE_USERSETTINGS))
                UserSettings.Load(FILE_USERSETTINGS);
            else
                UserSettings.Save(FILE_USERSETTINGS);
        }

        /// <summary>
        /// Gets case-insensitive user by name or returns null
        /// </summary>
        public Avatar GetUser(string name)
        {
            foreach (var user in Users)
                if (user.Name.ToLower() == name.ToLower())
                    return user;

            return null;
        }

        /// <summary>
        /// Gets user by session number or returns null
        /// </summary>
        public Avatar GetUser(int session)
        {
            foreach (var user in Users)
                if (user.Session == session)
                    return user;

            return null;
        }

        /// <summary>
        /// Gets user settings for a given user
        /// </summary>
        public IConfig GetUserSettings(Avatar av)
        {
            var sanitized = av.Name
                .Replace("]", "__")
                .Replace("[", "__");

            return UserSettings.Configs[sanitized] ?? UserSettings.AddConfig(sanitized);
        }

        /// <summary>
        /// Gets user settings for a given user name
        /// </summary>
        public IConfig GetUserSettings(string name)
        {
            var sanitized = name
                .Replace("]", "__")
                .Replace("[", "__");

            return UserSettings.Configs[sanitized] ?? UserSettings.AddConfig(sanitized);
        }

        void onAvatarAdd(Instance sender, Avatar avatar)
        {
            Log.Info("Users", "User {0} has entered", avatar.Name);
            Users.Add(avatar);

            // Do not load settings for bots else only add to unique user counts if name
            // is not present
            if (avatar.IsBot) Bots++;
            else if (GetUser(avatar.Name) != null) UniqueUsers++;
        }

        void onAvatarDelete(Instance sender, Avatar avatar)
        {
            Log.Info("Users", "User {0} has exited", avatar.Name);

            var user = GetUser(avatar.Session);
            if (user == null) return;
            else Users.Remove(user);

            if (avatar.IsBot) Bots--;
            else if (GetUser(avatar.Name) == null) UniqueUsers--;
        }

        void onAvatarsChange(Instance sender, Avatar avatar)
        {
            var user = GetUser(avatar.Session);
            if (user == null) return;
            else
                user.Position = avatar.Position;
        }
    }
}
