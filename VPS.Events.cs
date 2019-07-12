using System;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public delegate void AvatarArgs(Instance bot, Avatar<Vector3> user);

        public event AvatarArgs AvatarEnter;
        public event AvatarArgs AvatarLeave;
        public event AvatarArgs AvatarChange;

        public void SetupEvents()
        {
            Bot.OnAvatarEnter += onAvatarAdd;
            Bot.OnAvatarLeave += onAvatarLeave;
            Bot.OnAvatarChange += onAvatarsChange;
        }

        public void ClearEvents()
        {
            Bot.OnAvatarEnter -= onAvatarAdd;
            Bot.OnAvatarLeave -= onAvatarLeave;
            Bot.OnAvatarChange -= onAvatarsChange;

            AvatarEnter = null;
            AvatarLeave = null;
            AvatarChange = null;
        }

        #region Event handlers
        void onAvatarAdd(Instance sender, AvatarEnterEventArgsT<Avatar<Vector3>, Vector3> args)
        {
            lock (SyncMutex)
                Users.Add(args.Avatar);

            if (AvatarEnter != null)
            {
                AvatarEnter(sender, args.Avatar);
            }
        }

        void onAvatarLeave(Instance sender, AvatarLeaveEventArgsT<Avatar<Vector3>, Vector3> args)
        {
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
        #endregion
    }
}
