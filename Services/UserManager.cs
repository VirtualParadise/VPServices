using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP.Core;
using VP.Core.EventData;
using VP.Core.Structs;

namespace VPServices.Services
{
    public class User
    {
        public Stack<Vector3> TeleportHistory = new Stack<Vector3>();
        public Vector3 LastLocation;
        public Avatar Avatar;

        public string Name { get { return Avatar.Name; } }
        public int Session { get { return Avatar.Session; } }
    }

    class UserManager : List<User>
    {
        public const int TELEPORT_THRESHOLD = 20;

        public UserManager()
        {
            VPServices.Bot.EventAvatarAdd += OnAvatarAdd;
            VPServices.Bot.EventAvatarDelete += OnAvatarDelete;
            VPServices.Bot.EventAvatarChange += OnAvatarChange;
        }

        /// <summary>
        /// Gets user by name or returns null
        /// </summary>
        public User this[string name]
        {
            get
            {
                foreach (var user in this)
                    if (user.Name == name)
                        return user;

                return null;
            }
        }

        /// <summary>
        /// Gets user by session number or returns null
        /// </summary>
        public User this[int session]
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
            VPServices.UserMon.WriteLine("enter,{0},{1}",
                avatar.Name,
                DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
                );

            this.Add(new User
            {
                Avatar = avatar,
                LastLocation = new Vector3(avatar.X, avatar.Y, avatar.Z),
            });
        }

        public void OnAvatarDelete(Instance sender, Avatar avatar)
        {
            VPServices.UserMon.WriteLine("leave,{0},{1}",
                avatar.Name,
                DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
                );

            this.Remove(this[avatar.Session]);
        }

        public void OnAvatarChange(Instance sender, Avatar avatar)
        {
            var user = this[avatar.Session];
            var ll = user.LastLocation;
            var nl = new Vector3(avatar.X, avatar.Y, avatar.Z);
            user.Avatar = avatar;
            
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
            if (user.TeleportHistory.Count == 0)
            {
                bot.Say(string.Format("{0}: No teleport history", who.Name));
                return;
            }

            var jump = user.TeleportHistory.Pop();
            bot.CallbackObjectAdd += VPServices.Jumps.OnJumpAddCallback;
            bot.AddObject(new VpObject
            {
                Model = "zcomp1.rwx",
                Description = string.Format(Jumps.JUMPBACK_DESC),
                Action = string.Format(Jumps.JUMP_ACTION, jump.X, jump.Y, jump.Z, 0),
                Position = new Vector3(who.X, who.Y + 0.025f, who.Z)
            });

            return;
        }
    }
}
