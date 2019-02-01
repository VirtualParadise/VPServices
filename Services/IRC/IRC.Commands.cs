using System;
using System.Linq;
using VpNet;
using VPServices.Extensions;

namespace VPServices.Services
{
    partial class IRC : IService
    {
        bool cmdIRCConnect(VPServices app, Avatar<Vector3> who, string data)
        {
            lock (mutex)
            {
                if (irc.IsConnected)
                {
                    app.Warn(who.Session, msgAlreadyConnected, config.Channel, config.Host);
                    return true;
                }

                connect(app);
                return true;
            }
        }

        bool cmdIRCDisconnect(VPServices app, Avatar<Vector3> who, string data)
        {
            lock (mutex)
            {
                if (!irc.IsConnected)
                {
                    app.Warn(who.Session, msgNotConnected);
                    return true;
                }

                disconnect(app);
                return true;
            }
        }

        bool cmdMute(VPServices app, Avatar<Vector3> who, string target, bool muting)
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
