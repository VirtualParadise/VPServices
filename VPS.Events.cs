using System;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public delegate void AvatarArgs(Instance bot, Avatar<Vector3> user);
        //public delegate void ChatArgs(Instance bot, Avatar<Vector3> user, string message);

        public event AvatarArgs AvatarEnter;
        public event AvatarArgs AvatarLeave;
        public event AvatarArgs AvatarChange;
        //public event ChatArgs Chat;

        public void SetupEvents()
        {
            Bot.OnAvatarEnter += onAvatarAdd;
            Bot.OnAvatarLeave += onAvatarLeave;
            Bot.OnAvatarChange += onAvatarsChange;
            //Bot.OnChatMessage += onChat;
        }

        public void ClearEvents()
        {
            Bot.OnAvatarEnter -= onAvatarAdd;
            Bot.OnAvatarLeave -= onAvatarLeave;
            Bot.OnAvatarChange -= onAvatarsChange;
            //Bot.OnChatMessage -= onChat;

            AvatarEnter = null;
            AvatarLeave = null;
            AvatarChange = null;
            //Chat = null;
        }

        #region Event handlers
        void onAvatarAdd(Instance sender, AvatarEnterEventArgsT<Avatar<Vector3>, Vector3> args)
        {
            sender.ConsoleMessage(string.Format("*** {0} [SID#{1}] enters", args.Avatar.Name, args.Avatar.Session), new Color(0, 0, 128));

            lock (SyncMutex)
                Users.Add(args.Avatar);

            if (AvatarEnter != null)
            {
                AvatarEnter(sender, args.Avatar);
            }
        }

        void onAvatarLeave(Instance sender, AvatarLeaveEventArgsT<Avatar<Vector3>, Vector3> args)
        {
            sender.ConsoleMessage(string.Format("*** {0} [SID#{1}] leaves", args.Avatar.Name, args.Avatar.Session), new Color(0, 0, 128));

            var user = GetUser(args.Avatar.Session);

            if (AvatarLeave != null)
            {
                AvatarLeave(sender, args.Avatar);
            }

            lock (SyncMutex)
                Users.Remove(user);
        }

        void onAvatarsChange(Instance sender, AvatarChangeEventArgsT<Avatar<Vector3>, Vector3> args)
        {
            var user = GetUser(args.Avatar.Session);
            user.Position = args.Avatar.Position;

            if (AvatarChange != null)
            {
                AvatarChange(sender, args.Avatar);
            }
        } 

        //void onChat(Instance sender, ChatMessageEventArgsT<Avatar<Vector3>, ChatMessage, Vector3, Color> args)
        //{
        //    var user = GetUser(args.Avatar.Session);
        //    if ( user == null )
        //        return;

        //    if (args.ChatMessage != null)
        //    {
        //        Chat(sender, user, args.ChatMessage.Message);
        //    }
        //}
        #endregion
    }
}
