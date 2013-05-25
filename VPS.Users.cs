using Nini.Config;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        /// <summary>
        /// Global list of currently present users
        /// </summary>
        public List<Avatar> Users = new List<Avatar>();

        public void SetupUsers()
        {
            migrateUsers();
        }

        #region Migrations
        /// <summary>
        /// Applies any migrational changes from older to newer versions
        /// </summary>
        void migrateUsers()
        {
            var target = CoreSettings.GetInt("Version", 0) + 1;

            switch ( target )
            {
                case 1:
                    migSetupUserSQLite();
                    migUserIniToSQLite();
                    break;
            }
        }

        void migSetupUserSQLite()
        {
            Connection.CreateTable<sqlUserSettings>();
            Connection.Execute(
                @"CREATE TRIGGER DeleteOldUserSetting
                BEFORE INSERT ON UserSettings 
                FOR EACH ROW 
                BEGIN 
                DELETE FROM UserSettings WHERE UserID = new.UserID AND Name = new.Name
                END");

            Log.Debug("Users", "Created SQLite tables for user settings");
        }

        void migUserIniToSQLite()
        {
            var userIni = "UserSettings.ini";

            if ( !File.Exists(userIni) )
                return;

            var configSource = new IniConfigSource(userIni);
            foreach (IConfig config in configSource.Configs)
            {
                // Discard bot settings
                if ( config.Name.StartsWith("__") )
                {
                    Log.Fine("Migration", "Found config for bot '{0}'; discarding", config.Name);
                    continue;
                }

            promptUserID:
                Console.WriteLine("\n[Migrations] What user ID is user '{0}'?", config.Name);
                Console.Write("> ");
                var givenId = Console.ReadLine();
                int id;

                if ( !int.TryParse(givenId, out id) )
                    goto promptUserID;

                Connection.BeginTransaction();
                foreach ( var key in config.GetKeys() )
                    Connection.Insert( new sqlUserSettings
                    {
                        UserID = id,
                        Name    = key,
                        Value  = config.Get(key)
                    });
                Connection.Commit();
            }

            var backup = userIni + ".bak";
            File.Move(userIni, backup);
            Log.Debug("Users", "Migrated INI user settings to SQLite; backed up to '{0}'", backup);
        } 
        #endregion

        #region User getters
        /// <summary>
        /// Gets all known sessions of a given case-insensitive user name
        /// </summary>
        public Avatar[] GetUsers(string name)
        {
            var query = from   u in Users
                        where  u.Name.IEquals(name)
                        select u;

            return query.ToArray();
        }

        /// <summary>
        /// Gets case-insensitive user by name or returns null
        /// </summary>
        public Avatar GetUser(string name)
        {
            return GetUsers(name).FirstOrDefault();
        }

        /// <summary>
        /// Gets user by session number or returns null
        /// </summary>
        public Avatar GetUser(int session)
        {
            var query = from   u in Users
                        where  u.Session == session
                        select u;

            return query.FirstOrDefault();
        } 
        #endregion
    }

    static class AvatarExtensions
    {
        /// <summary>
        /// Gets a user setting of the specified key as a string, or returns null if
        /// not set
        /// </summary>
        public static string GetSetting(this Avatar user, string key)
        {
            var query = from   s in VPServices.App.Connection.Table<sqlUserSettings>()
                        select s;

            if (query.Count() <= 0)
                return null;
            else
                return query.First().Value;
        }

        public static int GetSettingInt(this Avatar user, string key, int defValue = 0)
        {
            var setting = GetSetting(user, key);
            int value;

            if ( setting == null || !int.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public static bool GetSettingBool(this Avatar user, string key, bool defValue = false)
        {
            var  setting = GetSetting(user, key);
            bool value;

            if ( setting == null || !bool.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public static DateTime GetSettingDateTime(this Avatar user, string key)
        {
            var      setting = GetSetting(user, key);
            DateTime value;

            if ( setting == null || !DateTime.TryParse(setting, out value) )
                return TDateTime.UnixEpoch;
            else
                return value;
        }

        public static void SetSetting(this Avatar user, string key, object value)
        {
            VPServices.App.Connection.InsertOrReplace(new sqlUserSettings
            {
                UserID = user.Id,
                Name   = key,
                Value  = value.ToString()
            });
        }

        public static void DeleteSetting(this Avatar user, string key)
        {
            VPServices.App.Connection.Execute("DELETE FROM UserSettings WHERE UserID = ? AND Name = ?", user.Id, key);
        }
    }

    [Table("UserSettings")]
    class sqlUserSettings
    {
        [Indexed]
        public int    UserID { get; set; }
        [Indexed]
        public string Name   { get; set; }
        public string Value  { get; set; }
    }
}