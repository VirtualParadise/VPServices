using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        void onIRCMessage(object sender, IrcRawMessageEventArgs e)
        {
            if ( config.DebugProtocol )
                Log.Fine(Name, "Protocol message: {0}", e.RawContent);

            var bot = VPServices.App.Bot;
            if ( e.Message.Parameters[0] == config.Channel )
            {
                if ( e.Message.Command.IEquals("PRIVMSG") )
                {
                    var msg = e.Message.Parameters[1];

                    if (msg[0] == ircAction)
                    {
                        msg = msg.Trim(ircAction);
                        msg = msg.Remove(0, 7);
                        messageToVP(false, "", "{0} {1}", e.Message.Source.Name, msg);
                    }
                    else
                        messageToVP(false, e.Message.Source.Name, msg);
                }
                else if ( e.Message.Command.IEquals("JOIN") )
                    messageToVP(true, "", msgEntry, e.Message.Source.Name, config.Channel);
                else if ( e.Message.Command.IEquals("PART") )
                    messageToVP(true, "", msgPart, e.Message.Source.Name, config.Channel);

                return;
            }

            if ( e.Message.Command.IEquals("QUIT") )
                messageToVP(true, "", msgQuit, e.Message.Source.Name, e.Message.Parameters[0]);
        }

        void messageToVP(bool announce, string name, string message, params object[] parts)
        {
            var fx    = announce ? ChatEffect.Italic : ChatEffect.None;
            var color = announce ? VPServices.ColorInfo : colorChat;

            foreach (var user in VPServices.App.Users)
            {
                // No broadcasting to those muting IRC
                if ( user.GetSettingBool(settingMuteIRC) )
                    continue;

                var muteList = user.GetSetting(settingMuteList);
                var muted    = ( muteList ?? "" ).TerseSplit(',').ToList();

                // No broadcasting to those muting target user
                if ( muted.Contains(name) )
                    continue;

                VPServices.App.Bot.ConsoleMessage(user.Session, fx, color, name, message, parts);
            }
        }
    }
}
