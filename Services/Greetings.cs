using Serilog;
using System;
using System.Collections.Generic;
using VpNet;
using VPServices;
using VPServices.Extensions;

namespace VPServices.Services
{
    /// <summary>
    /// Handles user entry / exit announcements
    /// </summary>
    public class Greetings : IService
    {
        readonly ILogger logger = Log.ForContext("Tag", "Greetings");
        public string Name
        {
            get { return "Greetings"; }
        }

        public void Init(VPServices app, Instance bot)
        {
            app.Commands.Add(new Command(
                "Greetings: Show/hide", "^greet(ing)?s?$", (o, e, a) => { return cmdToggle(o, e, a, SettingShowGreets); },
                @"Toggles or sets whether or not the bot sends you user entry/exit messages",
                @"!greets `[true|false]`"
            ));

            app.Commands.Add(new Command(
                "Greetings: Greet me", "^greetme$", (o, e, a) => { return cmdToggle(o, e, a, SettingGreetMe); },
                @"Toggles or sets whether or not the bot should announce your entry and exit to other users",
                @"!greetme `[true|false]`"
            ));

            app.AvatarEnter += (b,a) => { doGreet(b, a, true);  };
            app.AvatarLeave += (b,a) => { doGreet(b, a, false); };
        }

        public void Migrate(VPServices app, int target) {  }
        public void Dispose() { }

        public const string SettingGreetMe    = "GreetMe";
        public const string SettingShowGreets = "GreetShow";

        const string msgEntry      = "*** {0} has entered {1}";
        const string msgExit       = "*** {0} has left {1}";
        const string msgShowGreets = "Entry/exit messages will now be shown to you";
        const string msgHideGreets = "Entry/exit messages will no longer be shown to you";
        const string msgGreetMe    = "You will now be announced on entry/exit";
        const string msgGreetMeNot = "You will no longer be announced on entry/exit";

        #region Public cross-plugin methods
        public bool CanGreet(Avatar<Vector3> who)
        {
            return who.GetSettingBool(SettingGreetMe, true);
        }
        #endregion

        #region Command handlers
        bool cmdToggle(VPServices app, Avatar<Vector3> who, string data, string key)
        {
            string msg    = null;
            bool   toggle;

            if ( data != "" )
            {
                // Try to parse user given boolean; reject command on failure
                if ( !VPServices.TryParseBool(data, out toggle) )
                    return false;
            }
            else
                toggle = !who.GetSettingBool(key);

            who.SetSetting(key, toggle);
            switch (key)
            {
                case SettingGreetMe:
                    msg = toggle ? msgGreetMe : msgGreetMeNot;
                    break;

                case SettingShowGreets:
                    msg = toggle ? msgShowGreets : msgHideGreets;
                    break;
            }

            app.Notify(who.Session, msg);
            logger.Debug("Toggled greet-me for {User} to {GreetMe}", who.Name, toggle);
            return true;
        }
        #endregion

        #region Event handlers
        void doGreet(Instance bot, Avatar<Vector3> who, bool entering)
        {
            // No greetings within 10 seconds of bot load, to prevent flooding of entries
            // on initial user list load
            if ( VPServices.App.LastConnect.SecondsToNow() < 10 )
                return;

            var app = VPServices.App;

            // Do not greet if GreetMe is false
            if ( !CanGreet(who) )
                return;

            lock (VPServices.App.SyncMutex)
            {
                foreach ( var target in app.Users )
                {
                    // Do not send greet to bots or to the entering user themselves
                    if ( target.IsBot || target.Session == who.Session )
                        continue;

                    // Only send greet if target wants them
                    if ( target.GetSettingBool(SettingShowGreets, true) )
                    {
                        var msg = entering ? msgEntry : msgExit;
                        bot.ConsoleMessage(target.Session, "", string.Format(msg, who.Name, app.World), VPServices.ColorInfo, TextEffectTypes.Italic);
                    }
                }
            }
        } 
        #endregion
    }
}
