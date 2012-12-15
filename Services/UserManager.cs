using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    public class ServicesUser
    {
        public Stack<Vector3> TeleportHistory = new Stack<Vector3>();
        public Vector3 LastLocation;
        public Avatar Avatar;

        public string Name { get { return Avatar.Name; } }
        public int Session { get { return Avatar.Session; } }
    }

    class UserManager : List<ServicesUser>
    {
        public const int TELEPORT_THRESHOLD = 20;
        public int UniqueUsers = 0;
        public int Bots = 0;

        public UserManager()
        {
            VPServices.Bot.World.AvatarAdd += OnAvatarAdd;
            VPServices.Bot.World.AvatarDelete += OnAvatarDelete;
            VPServices.Bot.World.AvatarChange += OnAvatarChange;
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

        public void OnAvatarAdd(Instance sender, Avatar avatar)
        {
            if (VPServices.KickBans.IsKickBanned(avatar.Name))
            {
                VPServices.KickBans.Eject(avatar.Session);
                return;
            }

            VPServices.UserMon.WriteLine("enter,{0},{1}",
                avatar.Name,
                DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
                );

            this.Add(new ServicesUser
            {
                Avatar = avatar,
                LastLocation = new Vector3
                {
                    X = avatar.X,
                    Y = avatar.Y,
                    Z = avatar.Z
                }
            });

            if (avatar.IsBot)
                Bots++;
            else if (this[avatar.Name] != null)
                UniqueUsers++;
        }

        public void OnAvatarDelete(Instance sender, Avatar avatar)
        {
            VPServices.UserMon.WriteLine("leave,{0},{1}",
                avatar.Name,
                DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
                );

            this.Remove(this[avatar.Session]);

            if (avatar.IsBot)
                Bots--;
            else if (this[avatar.Name] == null)
                UniqueUsers--;
        }

        public void OnAvatarChange(Instance sender, Avatar avatar)
        {
            var user = this[avatar.Session];
            if (user == null) return;
            user.Avatar = avatar;
            var ll = user.LastLocation;
            var nl = new Vector3
            {
                X = avatar.X,
                Y = avatar.Y,
                Z = avatar.Z
            };
            
            if (Math.Abs(avatar.X - ll.X) > TELEPORT_THRESHOLD
                || Math.Abs(avatar.Y - ll.Y) > (TELEPORT_THRESHOLD * 2)
                || Math.Abs(avatar.Z - ll.Z) > TELEPORT_THRESHOLD)
            {
                Console.WriteLine("Teleport history recorded for {0}", avatar.Name);
                user.TeleportHistory.Push(ll);
            }
            
            user.LastLocation = nl;
        }

        public void CmdGoBack(Instance bot, Avatar who, string data)
        {
            var user = this[who.Session];
            if (user == null) return;

            if (user.TeleportHistory.Count == 0)
            {
                bot.Comms.Say("{0}: No teleport history", who.Name);
                return;
            }

            var jump = user.TeleportHistory.Pop();
            bot.World.TeleportAvatar(
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
    }
}
