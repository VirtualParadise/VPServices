using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using VP;
using VPServices.Internal;

namespace VPServices
{
    public class User
    {
        const string tag = "User";

        public readonly UserSettings Settings;
        public readonly UserMessages Send;

        public Avatar Avatar;
        public World  World;

        public string Name
        {
            get { return Avatar.Name; }
        }

        public int Session
        {
            get { return Avatar.Session; }
        }

        public string[] Rights
        {
            get
            {
                var rights = VPServices.Settings.Rights[Name];

                if (rights == null)
                    return null;
                else
                    return rights.TerseSplit(',');
            }
        }

        public User(Avatar avatar, World world)
        {
            this.Avatar   = avatar;
            this.World    = world;
            this.Settings = new UserSettings(this);
            this.Send     = new UserMessages(this);

            Log.Fine(tag, "Created user for avatar '{0}' SID#{1} in world {2}", avatar, avatar.Session, World);
        }

        public override string ToString()
        {
            return Name;
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
