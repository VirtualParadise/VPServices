using System;
using VP;

namespace VPServices
{
    public delegate void ServicesChatArgs(World world, Avatar user, string message);
    public delegate void ServicesAvatarArgs(World world, Avatar user);

    public class EventManager
    {
        public event ServicesAvatarArgs AvatarEnter;
        public event ServicesAvatarArgs AvatarLeave;
        public event ServicesAvatarArgs AvatarChange;
        public event ServicesChatArgs   Chat;

        public void Setup()
        {
            
        }

        public void TakeDown()
        {
            AvatarEnter  = null;
            AvatarLeave  = null;
            AvatarChange = null;
            Chat         = null;
        }

        #region Event handlers
        void onAvatarAdd(World sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} [SID#{1}] enters", avatar.Name, avatar.Session);
            
            lock (SyncMutex)
                Users.Add(avatar);

            if ( AvatarEnter != null )
                AvatarEnter(sender, avatar);
        }

        void onAvatarLeave(World sender, Avatar avatar)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "*** {0} [SID#{1}] leaves", avatar.Name, avatar.Session);

            var user = GetUser(avatar.Session);
            if ( AvatarLeave != null )
                AvatarLeave(sender, avatar);

            lock (SyncMutex)
                Users.Remove(user);
        }

        void onAvatarsChange(World sender, Avatar avatar)
        {
            var user = GetUser(avatar.Session);
            user.Position = avatar.Position;

            if ( AvatarChange != null )
                AvatarChange(sender, avatar);
        } 

        void onChat(World sender, ChatMessage chat)
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
