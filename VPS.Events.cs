using System;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public delegate void AvatarArgs(VirtualParadiseClient bot, Avatar user);

        public event AvatarArgs AvatarEnter;
        public event AvatarArgs AvatarLeave;
        public event AvatarArgs AvatarChange;

        public void SetupEvents()
        {
            Bot.AvatarEntered += onAvatarAdd;
            Bot.AvatarLeft += onAvatarLeave;
            Bot.AvatarChanged += onAvatarsChange;
        }

        public void ClearEvents()
        {
            Bot.AvatarEntered -= onAvatarAdd;
            Bot.AvatarLeft -= onAvatarLeave;
            Bot.AvatarChanged -= onAvatarsChange;

            AvatarEnter = null;
            AvatarLeave = null;
            AvatarChange = null;
        }

        #region Event handlers
        void onAvatarAdd(VirtualParadiseClient sender, AvatarEnterEventArgs args)
        {
            lock (SyncMutex)
                Users.Add(args.Avatar);

            if (AvatarEnter != null)
            {
                AvatarEnter(sender, args.Avatar);
            }
        }

        void onAvatarLeave(VirtualParadiseClient sender, AvatarLeaveEventArgs args)
        {
            var user = GetUser(args.Avatar.Session);

            if (AvatarLeave != null)
            {
                AvatarLeave(sender, args.Avatar);
            }

            lock (SyncMutex)
                Users.Remove(user);
        }

        void onAvatarsChange(VirtualParadiseClient sender, AvatarChangeEventArgs args)
        {
            if (AvatarChange != null)
            {
                AvatarChange(sender, args.Avatar);
            }
        } 
        #endregion
    }
}
