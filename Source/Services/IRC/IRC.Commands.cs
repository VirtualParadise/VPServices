using System;
using System.Linq;
using VP;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        bool cmdIRCConnect(VPServices app, Avatar who, string data)
        {
            switch (state)
            {
                case IRCState.Connected:
                    app.Warn(who.Session, msgAlreadyConnected, config.Channel, config.Host);
                    return true;

                case IRCState.Connecting:
                    Log.Warn(Name, "Forcing disconnect and reconnect during connecting state");
                    state = IRCState.Disconnected;
                    irc.Disconnect();
                        
                    connect(app);
                    return true;

                case IRCState.Disconnecting:
                    app.Warn(who.Session, msgDisconnecting);
                    return true;

                default:
                case IRCState.Disconnected:
                    connect(app);
                    return true;
            }
        }

        bool cmdIRCDisconnect(VPServices app, Avatar who, string data)
        {
            switch (state)
            {
                case IRCState.Disconnecting:
                    app.Warn(who.Session, msgDisconnecting);
                    return true;

                case IRCState.Disconnected:
                    app.Warn(who.Session, msgNotConnected);
                    return true;

                case IRCState.Connecting:
                    app.Warn(who.Session, msgCantInterrupt);
                    return true;

                default:
                case IRCState.Connected:
                    disconnect(app);
                    return true;
            }
        }

        bool cmdMute(VPServices app, Avatar who, string target, bool muting)
        {
            // Mute IRC
            if (target == "")
            {
                who.SetSetting(settingMuteIRC, muting);
                app.Notify(who.Session, msgMuteIRC, muting ? "hidden from" : "shown to");
                return true;
            }

            // Reject invalid names
            if ( target.Contains(',') )
            {
                app.Warn(who.Session, "Cannot mute that name; commas not allowed");
                return true;
            }

            var muteList = who.GetSetting(settingMuteList);
            var muted    = ( muteList ?? "" ).TerseSplit(',').ToList();
            target       = target.ToLower();

            if (muting)
            {
                if ( muted.Contains(target) )
                {
                    app.Warn(who.Session, msgMuted, "already");
                    return true;
                }
                
                muted.Add(target);
                app.Notify(who.Session, msgMuteUser, target, "hidden");
            }
            else
            {
                if ( !muted.Contains(target) )
                {
                    app.Warn(who.Session, msgMuted, "not");
                    return true;
                }
                
                muted.Remove(target);
                app.Notify(who.Session, msgMuteUser, target, "shown");
            }

            muteList = string.Join(",", muted);
            who.SetSetting(settingMuteList, muteList);
            return true;
        }
    }
}
