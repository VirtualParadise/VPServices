using VP;

namespace VPServices
{
    static class Respond
    {
        public static void Warn(int session, string msg, params object[] subst)
        {
            var user = VPServices.Users.BySession(session);
            
            if (user == null)
                return;
            else
                user.World.Bot.ConsoleMessage(session, ChatEffect.None, Colors.Warn, "Services", msg, subst);
        }
    }
}
