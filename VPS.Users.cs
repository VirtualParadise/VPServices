using Nini.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VP;

namespace VPServices
{
    public partial class VPServices
    {
        const string defaultFileUserSettings = "UserSettings.ini";

        /// <summary>
        /// User settings INI
        /// </summary>
        public IniConfigSource UserSettings = new IniConfigSource { AutoSave = true };

        /// <summary>
        /// Global list of currently present users
        /// </summary>
        public List<Avatar> Users = new List<Avatar>();

        public int UniqueUsers = 0;
        public int Bots        = 0;

        public void SetupUserSettings()
        {
            Bot.Avatars.Enter  += onAvatarAdd;
            Bot.Avatars.Leave  += onAvatarDelete;
            Bot.Avatars.Change += onAvatarsChange;

            if ( File.Exists(defaultFileUserSettings) )
                UserSettings.Load(defaultFileUserSettings);
            else
                UserSettings.Save(defaultFileUserSettings);

            migrate();
        }

        /// <summary>
        /// Applies any migrational changes from older to newer versions
        /// </summary>
        void migrate()
        {
            var newConfigSource = new IniConfigSource();
            newConfigSource.Merge(UserSettings);

            // For when user setting names were forced to lowercase
            foreach (IConfig config in newConfigSource.Configs)
                UserSettings.Configs[config.Name].Name = config.Name.ToLower();
        }

        /// <summary>
        /// Gets case-insensitive user by name or returns null  (e.g. phantom users/bots)
        /// </summary>
        public Avatar GetUser(string name)
        {
            foreach (var user in Users)
                if (user.Name.ToLower() == name.ToLower())
                    return user;

            return null;
        }

        /// <summary>
        /// Gets user by session number or returns null (e.g. phantom users/bots)
        /// </summary>
        public Avatar GetUser(int session)
        {
            foreach (var user in Users)
                if (user.Session == session)
                    return user;

            return null;
        }

        /// <summary>
        /// Gets or creates user settings for a given user name
        /// </summary>
        public IConfig GetUserSettings(string name)
        {
            var sanitized = name
                .Replace("]", "__")
                .Replace("[", "__")
                .ToLower();

            return UserSettings.Configs[sanitized] ?? UserSettings.AddConfig(sanitized);
        }

        /// <summary>
        /// Gets or creates user settings for a given user
        /// </summary>
        public IConfig GetUserSettings(Avatar av)
        {
            return GetUserSettings(av.Name);
        }

        void onAvatarAdd(Instance sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} enters", avatar.Name);
            Users.Add(avatar);

            // Do not load settings for bots else only add to unique user counts if name
            // is not present
            if      (avatar.IsBot)
                Bots++;
            else if ( GetUser(avatar.Name) != null )
                UniqueUsers++;
        }

        void onAvatarDelete(Instance sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} leaves", avatar.Name);

            var user = GetUser(avatar.Session);
            if (user == null)
                return;
            else
                Users.Remove(user);

            if (avatar.IsBot)
                Bots--;
            else if ( GetUser(avatar.Name) == null )
                UniqueUsers--;
        }

        void onAvatarsChange(Instance sender, Avatar avatar)
        {
            var user = GetUser(avatar.Session);
            if (user == null)
                return;
            else
                user.Position = avatar.Position;
        }
    }
}
