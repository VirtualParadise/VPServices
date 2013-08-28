using System;
using VP;
using AvatarArgs = VP.InstanceAvatars.AvatarArgs;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public delegate void ChatArgs(Instance bot, Avatar user, string message);

        public event AvatarArgs AvatarEnter;
        public event AvatarArgs AvatarLeave;
        public event AvatarArgs AvatarChange;
        public event ChatArgs   Chat;

        public void SetupEvents()
        {
            Bot.Avatars.Enter  += onAvatarAdd;
            Bot.Avatars.Leave  += onAvatarLeave;
            Bot.Avatars.Change += onAvatarsChange;
            Bot.Chat           += onChat;
        }

        public void ClearEvents()
        {
            Bot.Avatars.Enter  -= onAvatarAdd;
            Bot.Avatars.Leave  -= onAvatarLeave;
            Bot.Avatars.Change -= onAvatarsChange;
            Bot.Chat           -= onChat;

            AvatarEnter  = null;
            AvatarLeave  = null;
            AvatarChange = null;
            Chat         = null;
        }

        #region Event handlers
        void onAvatarAdd(Instance sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} [SID#{1}] enters", avatar.Name, avatar.Session);
            
            lock (SyncMutex)
                Users.Add(avatar);

            if ( AvatarEnter != null )
                AvatarEnter(sender, avatar);
        }

        void onAvatarLeave(Instance sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} [SID#{1}] leaves", avatar.Name, avatar.Session);

            var user = GetUser(avatar.Session);
            if ( AvatarLeave != null )
                AvatarLeave(sender, avatar);

            lock (SyncMutex)
                Users.Remove(user);
        }

        void onAvatarsChange(Instance sender, Avatar avatar)
        {
            var user = GetUser(avatar.Session);
            user.Position = avatar.Position;

            if ( AvatarChange != null )
                AvatarChange(sender, avatar);
        } 

        void onChat(Instance sender, ChatMessage chat)
        {
            var user = GetUser(chat.Session);
            if ( user == null )
                return;

            if (Chat != null)
                Chat(sender, user, chat.Message);
        }
        #endregion
    }
}
