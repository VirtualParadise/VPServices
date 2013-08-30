using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices
{
    class User
    {
        public Avatar Avatar;
        public World  World;

        public Dictionary<string, string> GetSettings()
        {
            var conn  = VPServices.Data.SQL;
            var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? ORDER BY Name ASC", Avatar.Id);
            var dict  = new Dictionary<string, string>();

            foreach (var entry in query)
                dict.Add(entry.Name, entry.Value);

            return dict;
        }

        public string GetSetting(string key)
        {
            var conn  = VPServices.Data.SQL;
            var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? AND Name = ? COLLATE NOCASE", Avatar.Id, key);

            if (query.Count() <= 0)
                return null;
            else
                return query.First().Value;
        }

        public int GetSettingInt(string key, int defValue = 0)
        {
            var setting = GetSetting(key);
            int value;

            if ( setting == null || !int.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public bool GetSettingBool(string key, bool defValue = false)
        {
            var  setting = GetSetting(key);
            bool value;

            if ( setting == null || !bool.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public DateTime GetSettingDateTime(string key)
        {
            var      setting = GetSetting(key);
            DateTime value;

            if ( setting == null || !DateTime.TryParse(setting, out value) )
                return TDateTime.UnixEpoch;
            else
                return value;
        }

        public void SetSetting(string key, object value)
        {
            VPServices.Data.SQL.InsertOrReplace(new sqlUserSettings
            {
                UserID = Avatar.Id,
                Name   = key,
                Value  = value.ToString()
            });
        }

        public void DeleteSetting(this Avatar user, string key)
        {
            VPServices.Data.SQL.Execute("DELETE FROM UserSettings WHERE UserID = ? AND Name = ?", user.Id, key);
        }
    }
}
