using SQLite;
using System;
using System.Collections.Generic;
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
        public static Dictionary<string, string> GetSettings(this Avatar user)
        {
            var conn  = VPServices.App.Connection;
            var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? ORDER BY Name ASC", user.Id);
            var dict  = new Dictionary<string, string>();

            foreach (var entry in query)
                dict.Add(entry.Name, entry.Value);

            return dict;            
        }

        /// <summary>
        /// Gets a user setting of the specified key as a string, or returns null if
        /// not set
        /// </summary>
        public static string GetSetting(this Avatar user, string key)
        {
            try
            {
                var conn  = VPServices.App.Connection;
                var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? AND Name = ? COLLATE NOCASE", user.Id, key);

                if (query.Count() <= 0)
                    return null;
                else
                    return query.First().Value;
            }
            catch (Exception e)
            {
                Log.Severe("Users", "Could not get setting '{0}' for ID {1}", key, user.Id);
                e.LogFullStackTrace();

                return null;
            }
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
        [MaxLength(100000)]
        public string Value  { get; set; }
    }
}