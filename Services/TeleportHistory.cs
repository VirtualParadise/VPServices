using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    class TeleportHistory : IService
    {
        const int TELEPORT_THRESHOLD = 20;

        Dictionary<int, History> histories = new Dictionary<int, History>();

        public string Name { get { return "Teleport history"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command("Go back", "^(ba?ck|prev)$", cmdGoBack,
                @"Teleports the requester to their previous position in their teleport history"),
                new Command("Go forward", "^(forward|fwd)$", cmdGoForward,
                @"Teleports the requester forward through their teleport history"),
            });

            bot.Avatars.Enter += onEnter;
            bot.Avatars.Leave += onLeave;
            bot.Avatars.Change += onChange;
        }

        public void Dispose()
        {
            histories.Clear();
        }

        void onEnter(Instance sender, Avatar avatar)
        {
            histories[avatar.Session] = new History();
            histories[avatar.Session].LastLocation = avatar.Position;
        }

        void onLeave(Instance sender, Avatar avatar)
        {
            histories[avatar.Session].Back.Clear();
            histories.Remove(avatar.Session);
        }

        /// <summary>
        /// Tracks user movements for teleport history
        /// </summary>
        public void onChange(Instance sender, Avatar avatar)
        {
            var history = histories[avatar.Session];
            var ll = history.LastLocation;
            
            if (Math.Abs(avatar.X - ll.X) > TELEPORT_THRESHOLD
                || Math.Abs(avatar.Y - ll.Y) > (TELEPORT_THRESHOLD * 2)
                || Math.Abs(avatar.Z - ll.Z) > TELEPORT_THRESHOLD)
            {
                if (history.IgnoreNextChange)
                    history.IgnoreNextChange = false;
                else
                {
                    history.Back.Push(ll);
                    history.Forward.Clear();
                    Log.Fine(Name, "Teleport history recorded for {0}", avatar.Name);
                }
            }

            history.LastLocation = avatar.Position;
        }

        /// <summary>
        /// Handles the !back command
        /// </summary>
        public void cmdGoBack(VPServices serv, Avatar user, string data)
        {
            var history = histories[user.Session];
            if (history.Back.Count == 0)
            {
                serv.Bot.Say("{0}: No back teleport history", user.Name);
                Log.Info(Name, "Rejecting back command for {0} due to lack of teleport history", user.Name);
                return;
            }

            var pos = history.Back.Pop();
            serv.Bot.Avatars.Teleport(user.Session, "", new Vector3(pos.X, pos.Y, pos.Z), pos.Yaw, pos.Pitch);
            history.Forward.Push(user.Position);
            history.IgnoreNextChange = true;
        }

        public void cmdGoForward(VPServices serv, Avatar user, string data)
        {
            var history = histories[user.Session];
            if (history.Forward.Count == 0)
            {
                serv.Bot.Say("{0}: No forward teleport history", user.Name);
                Log.Info(Name, "Rejecting forward command for {0} due to lack of teleport history", user.Name);
                return;
            }

            var pos = history.Forward.Pop();
            serv.Bot.Avatars.Teleport(user.Session, "", new Vector3(pos.X, pos.Y, pos.Z), pos.Yaw, pos.Pitch);
        }
    }

    class History
    {
        public AvatarPosition LastLocation;
        public Stack<AvatarPosition> Back = new Stack<AvatarPosition>();
        public Stack<AvatarPosition> Forward = new Stack<AvatarPosition>();
        public bool IgnoreNextChange = false;
    }
}
