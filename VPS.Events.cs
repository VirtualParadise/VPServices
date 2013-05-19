using Nini.Config;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public event InstanceAvatars.AvatarArgs AvatarEnter;
        public event InstanceAvatars.AvatarArgs AvatarLeave;
        public event InstanceAvatars.AvatarArgs AvatarChange;

        public void SetupEvents()
        {
            Bot.Avatars.Enter  += onAvatarAdd;
            Bot.Avatars.Leave  += onAvatarLeave;
            Bot.Avatars.Change += onAvatarsChange;
        }

        public void ClearEvents()
        {
            Bot.Avatars.Enter  -= onAvatarAdd;
            Bot.Avatars.Leave  -= onAvatarLeave;
            Bot.Avatars.Change -= onAvatarsChange;

            AvatarEnter  = null;
            AvatarLeave  = null;
            AvatarChange = null;
        }

        void onAvatarAdd(Instance sender, Avatar avatar)
        {
            // Do not load settings for bots else only add to unique user counts if name
            // is not present
            if      (avatar.IsBot)
                Bots++;
            else if ( GetUser(avatar.Name) != null )
                UniqueUsers++;

            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} enters", avatar.Name);
            Users.Add(avatar);

            if (AvatarEnter != null)
                AvatarEnter(sender, avatar);
        }

        void onAvatarLeave(Instance sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} leaves", avatar.Name);

            var user = GetUser(avatar.Session);
            if (user == null)
                return;
            else
            {
                if (AvatarLeave != null)
                    AvatarLeave(sender, avatar);

                Users.Remove(user);
            }

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
            
            user.Position = avatar.Position;

            if (AvatarChange != null)
                AvatarChange(sender, avatar);

            //TODO: change over all uses of bot avatar events to VPS events
        }
    }
}
