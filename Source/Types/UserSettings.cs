using System;
using System.Collections.Generic;
using System.Linq;

namespace VPServices.Internal
{
    public class UserSettings
    {
        User user;

        public UserSettings(User user)
        {
            this.user = user;
        }

        public Dictionary<string, string> GetAll()
        {
            var conn  = VPServices.Data.SQL;
            var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? ORDER BY Name ASC", user.Avatar.Id);
            var dict  = new Dictionary<string, string>();

            foreach (var entry in query)
                dict.Add(entry.Name, entry.Value);

            return dict;
        }

        public string AsString(string key)
        {
            var conn  = VPServices.Data.SQL;
            var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? AND Name = ? COLLATE NOCASE", user.Avatar.Id, key);

            if (query.Count() <= 0)
                return null;
            else
                return query.First().Value;
        }

        public int AsInt(string key, int defValue = 0)
        {
            var setting = AsString(key);
            int value;

            if ( setting == null || !int.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public bool AsBool(string key, bool defValue = false)
        {
            var  setting = AsString(key);
            bool value;

            if ( setting == null || !bool.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public DateTime AsDateTime(string key)
        {
            var      setting = AsString(key);
            DateTime value;

            if ( setting == null || !DateTime.TryParse(setting, out value) )
                return TDateTime.UnixEpoch;
            else
                return value;
        }

        public void Set(string key, object value)
        {
            VPServices.Data.SQL.InsertOrReplace(new sqlUserSettings
            {
                UserID = user.Avatar.Id,
                Name   = key,
                Value  = value.ToString()
            });
        }

        public void Delete(string key)
        {
            VPServices.Data.SQL.Execute("DELETE FROM UserSettings WHERE UserID = ? AND Name = ?", user.Avatar.Id, key);
        }
    }
}
