using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        void onIRCMessage(object sender, IrcEventArgs e)
        {
            messageToVP(false, e.Data.Nick, "{0}", e.Data.Message);
        }

        void onIRCAction(object sender, ActionEventArgs e)
        {
            messageToVP(false, "", "{0} {1}", e.Data.Nick, e.ActionMessage);
        }

        void onIRCJoin(object sender, JoinEventArgs e)
        {
            messageToVP(true, "", msgEntry, e.Who, e.Channel);
        }

        void onIRCPart(object sender, PartEventArgs e)
        {
            messageToVP(true, "", msgPart, e.Who, e.Channel);
        }

        void onIRCQuit(object sender, QuitEventArgs e)
        {
            messageToVP(true, "", msgQuit, e.Who, e.QuitMessage);
        }

        void onIRCKick(object sender, KickEventArgs e)
        {
            messageToVP(true, "", msgKicked, e.Who, e.Whom, e.Channel, e.KickReason);

            if (e.Whom == config.NickName)
                irc.Disconnect();
        }

        void onIRCBan(object sender, BanEventArgs e)
        {
            messageToVP(true, "", msgBanned, e.Who, e.Hostmask, e.Channel);
        }

        void onIRCNick(object sender, NickChangeEventArgs e)
        {
            messageToVP(true, "", msgNick, e.OldNickname, e.NewNickname);
        }

        void messageToVP(bool announce, string name, string message, params object[] parts)
        {
            var fx    = announce ? ChatEffect.Italic : ChatEffect.None;
            var color = announce ? VPServices.ColorInfo : colorChat;
            message   = message.LFormat(parts);

            foreach (var user in VPServices.App.Users)
            {
                // No broadcasting to those muting IRC
                if ( user.GetSettingBool(settingMuteIRC) )
                    continue;

                var muteList = user.GetSetting(settingMuteList);
                var muted    = ( muteList ?? "" ).TerseSplit(',');

                // No broadcasting to those muting target user
                if ( muted.IContains(name) )
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
                        VPServices.App.Bot.ConsoleMessage(user.Session, fx, color, name, "{0}", line);
                }
                else
                    VPServices.App.Bot.ConsoleMessage(user.Session, fx, color, name, "{0}", message);
            }
        }
    }
}
