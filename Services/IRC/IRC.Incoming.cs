using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using VpNet;
using VPServices.Extensions;

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

        void onIRCJoin(object sender, Meebey.SmartIrc4net.JoinEventArgs e)
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
            var fx    = announce ? TextEffectTypes.Italic : (TextEffectTypes)0;
            var color = announce ? VPServices.ColorInfo : colorChat;
            message   = string.Format(message, parts);

            lock (VPServices.App.SyncMutex)
            {
                foreach (var user in VPServices.App.Users)
                {
                    // No broadcasting to those muting IRC
                    if ( user.GetSettingBool(settingMuteIRC) )
                        continue;

                    var muteList = user.GetSetting(settingMuteList);
                    var muted    = ( muteList ?? "" ).TerseSplit(',');

                    // No broadcasting to those muting target user
                    if (muted.Any(otherName => string.Equals(name, otherName, StringComparison.OrdinalIgnoreCase)))
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
                            VPServices.App.Bot.ConsoleMessage(user.Session, name, $"{line}", color, fx);
                    }
                    else
                        VPServices.App.Bot.ConsoleMessage(user.Session, name, $"{message}", color, fx);
                }
            }
        }
    }
}
