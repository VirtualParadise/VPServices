using IrcDotNet;
using System;
using System.Linq;
using System.Collections.Generic;
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
                else if ( e.Message.Command.IEquals("KICK") )
                {
                    messageToVP(true, "", msgKicked, e.Message.Parameters[1], config.Channel);

                    if (e.Message.Parameters[1] == config.Registration.NickName)
                        disconnect(VPServices.App);
                }

                return;
            }

            if ( e.Message.Command.IEquals("QUIT") )
                messageToVP(true, "", msgQuit, e.Message.Source.Name, e.Message.Parameters[0]);

            if ( e.Message.Command.IEquals("NICK") )
                messageToVP(true, "", msgNick, e.Message.Source.Name, e.Message.Parameters[0]);
            
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
                foreach (var mute in muted)
                    if ( mute.IEquals(name) )
                        continue;

                // Keep within VP message limit
                if (message.Length > 245)
                {
                    var messages = new List<string>();
                    var buffer   = message;

                    while (buffer.Length > 245)
                    {
                        var part = buffer.Substring(0, 245);
                        buffer   = buffer.Substring(245);

                        messages.Add(part);
                    }

                    messages.Add(buffer);
                    foreach (var line in messages)
                        VPServices.App.Bot.ConsoleMessage(user.Session, fx, color, name, line, parts);
                }
                else
                    VPServices.App.Bot.ConsoleMessage(user.Session, fx, color, name, message, parts);
            }
        }
    }
}
