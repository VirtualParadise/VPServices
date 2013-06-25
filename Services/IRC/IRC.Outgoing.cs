using Meebey.SmartIrc4net;
using System;
using VP;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        void onWorldChat(Instance sender, Avatar user, string message)
        {
            // No chat if not connected
            if (!irc.IsConnected)
                return;

            var msgRoll = message.TerseSplit("\n");

            foreach (var msg in msgRoll)
                if ( msg.StartsWith("/me ") )
                    irc.SendMessage(SendType.Action, config.Channel, user.Name + " " + msg.Substring(4) );
                else
                    irc.SendMessage(SendType.Message, config.Channel, user.Name + ": " +  msg );
        }

        void onWorldEnter(Instance sender, Avatar avatar)
        {
            if (!irc.IsConnected)
                return;

            var msg = msgEntry.LFormat(avatar.Name, VPServices.App.World);
            irc.SendMessage(SendType.Action, config.Channel, msg);
        }

        void onWorldLeave(Instance sender, Avatar avatar)
        {
            if (!irc.IsConnected)
                return;

            var msg = msgPart.LFormat(avatar.Name, VPServices.App.World);
            irc.SendMessage(SendType.Action, config.Channel, msg);
        }
    }
}
