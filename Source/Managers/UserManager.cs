using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices
{
    public class UserManager
    {
        List<User> users = new List<User>();

        public User[] All
        {
            get { return users.ToArray(); }
        }

        /// <summary>
        /// Gets all known sessions of a given case-insensitive user name
        /// </summary>
        public User[] ByName(string name)
        {
            var query = from   u in users
                        where  u.Avatar.Name.IEquals(name)
                        select u;

            return query.ToArray();
        }

        public User BySession(int session)
        {
            var query = from   u in users
                        where  u.Avatar.Session == session
                        select u;

            return query.FirstOrDefault();
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