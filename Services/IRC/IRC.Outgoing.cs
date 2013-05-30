using System;
using VP;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        void onWorldChat(Instance sender, Avatar user, string message)
        {
            // No chat if not connected
            if ( state != IRCState.Connected || !irc.IsConnected )
                return;

            // Ignore commands
            if ( message.StartsWith("!") )
                return;

            string msg = "";
            if ( message.StartsWith("/me ") )
                msg = "PRIVMSG {0} :{3}ACTION {1} {2}{3}".LFormat(config.Channel, user.Name, message.Substring(4), ircAction);
            else
                msg = "PRIVMSG {0} :{1}: {2}".LFormat(config.Channel, user.Name, message);

            irc.SendRawMessage(msg);
        }

        void onWorldLeave(Instance sender, Avatar avatar)
        {
            if ( state != IRCState.Connected || !irc.IsConnected )
                return;

            var msg = @"PRIVMSG {0} :{3}ACTION *** {1} has left {2}{3}"
                .LFormat(config.Channel, avatar.Name, VPServices.App.World, ircAction);

            irc.SendRawMessage(msg);
        }

        void onWorldEnter(Instance sender, Avatar avatar)
        {
            if ( state != IRCState.Connected || !irc.IsConnected )
                return;

            var msg = @"PRIVMSG {0} :{3}ACTION *** {1} has entered {2}{3}"
                .LFormat(config.Channel, avatar.Name, VPServices.App.World, ircAction);

            irc.SendRawMessage(msg);
        }
    }
}
