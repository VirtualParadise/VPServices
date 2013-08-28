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

        void onWorldConsole(Instance sender, ConsoleMessage console)
        {
            // No chat if not connected
            if (!irc.IsConnected)
                return;
            
            // Ignore nameless consoles
            if ( string.IsNullOrWhiteSpace(console.Name) )
                return;

            // Ignore Services bot messages
            if (console.Name == sender.Name)
                return;

            var msgRoll = console.Message.TerseSplit("\n");

            foreach (var msg in msgRoll)
                irc.SendMessage(SendType.Message, config.Channel, "C* " + console.Name + " " +  msg );
        }

        void onWorldEnter(Instance sender, Avatar avatar)
        {
            if (!irc.IsConnected)
                return;

            // No greetings within 10 seconds of bot load, to prevent flooding of entries
            // on initial user list load
            if ( VPServices.App.LastConnect.SecondsToNow() < 10 )
                return;

            // Reject for those who have greetme off
            var greets = app.GetService<Greetings>();
            if ( greets != null && !greets.CanGreet(avatar) )
                return;

            var msg = msgEntry.LFormat(avatar.Name, VPServices.App.World);
            irc.SendMessage(SendType.Action, config.Channel, msg);
        }

        void onWorldLeave(Instance sender, Avatar avatar)
        {
            if (!irc.IsConnected)
                return;

            // No greetings within 10 seconds of bot load, to prevent flooding of entries
            // on initial user list load
            if ( VPServices.App.LastConnect.SecondsToNow() < 10 )
                return;

            // Reject for those who have greetme off
            var greets = app.GetService<Greetings>();
            if ( greets != null && !greets.CanGreet(avatar) )
                return;

            var msg = msgPart.LFormat(avatar.Name, VPServices.App.World);
            irc.SendMessage(SendType.Action, config.Channel, msg);
        }
    }
}
