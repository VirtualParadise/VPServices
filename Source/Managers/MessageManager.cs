using Nexus.Graphics.Colors;
using System;
using VP;

namespace VPServices
{
    public delegate void IncomingArgs(User source, string message);
    public delegate void OutgoingArgs(User target, string message);

    public class MessageManager
    {
        const string tag = "Messages";

        public event IncomingArgs Incoming;
        public event OutgoingArgs Outgoing;

        public void Setup()
        {
            VPServices.Worlds.Added   += w => { w.Bot.Chat += onChat; };
            VPServices.Worlds.Removed += w => { w.Bot.Chat -= onChat; };
        }

        public void Takedown()
        {
            Incoming = null;
            Outgoing = null;
        }

        public void Send(User user, ColorRgb color, string msg, params object[] subst)
        {
            var message = msg.LFormat(subst);
   
            user.World.Bot.ConsoleMessage(user.Session, ChatEffect.None, color, VPServices.Name, message);

            Log.Fine(tag, "To '{0}@{1}' SID#{2}: {3}", user, user.World, user.Session, message);
            if (Outgoing != null)
                Outgoing(user, msg.LFormat(subst));
        }

        void onChat(Instance sender, ChatMessage chat)
        {
            var user = VPServices.Users.BySession(chat.Session);

            if (user == null)
                return;

            Log.Fine(tag, "From '{0}@{1}' SID#{2}: {3}", user, user.World, chat.Session, chat.Message);
            if (Incoming != null)
                Incoming(user, chat.Message);
        }
    }
}
