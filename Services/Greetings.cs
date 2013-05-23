using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Handles user entry / exit announcements
    /// </summary>
    public class Greetings : IService
    {
        public string Name
        {
            get { return "Greetings"; }
        }

        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Greetings: Show/hide", "^greet(ing)?s?$", (o,e,a) => { return cmdToggle(o,e,a, settingShowGreets); },
                    @"Toggles or sets whether or not the bot sends you user entry/exit messages",
                    @"!greets `[true|false]`"
                ),

                new Command
                (
                    "Greetings: Greet me", "^greetme$", (o,e,a) => { return cmdToggle(o,e,a, settingGreetMe); },
                    @"Toggles or sets whether or not the bot should announce your entry and exit to other users",
                    @"!greetme `[true|false]`"
                ),
            });

            app.AvatarEnter += (b,a) => { doGreet(b, a, true);  };
            app.AvatarLeave += (b,a) => { doGreet(b, a, false); };
        }

        public void Migrate(VPServices app, int target) {  }
        public void Dispose() { }

        const string settingGreetMe    = "GreetMe";
        const string settingShowGreets = "GreetShow";

        const string msgEntry      = "*** {0} has entered {1}";
        const string msgExit       = "*** {0} has left {1}";
        const string msgShowGreets = "Entry/exit messages will now be shown to you";
        const string msgHideGreets = "Entry/exit messages will no longer be shown to you";
        const string msgGreetMe    = "You will now be announced on entry/exit";
        const string msgGreetMeNot = "You will no longer be announced on entry/exit";

        #region Command handlers
        bool cmdToggle(VPServices app, Avatar who, string data, string key)
        {
            var    config = app.GetUserSettings(who);
            string msg    = null;
            bool   toggle = false;

            // Try to parse user given boolean; silently ignore on failure
            if ( data != "" )
            if ( !VPServices.TryParseBool(data, out toggle) )
                return false;

            config.Set(key, toggle);
            switch (key)
            {
                case settingGreetMe:
                    msg = toggle ? msgGreetMe : msgGreetMeNot;
                    break;

                case settingShowGreets:
                    msg = toggle ? msgShowGreets : msgHideGreets;
                    break;
            }

            app.Notify(who.Session, msg);
            return Log.Debug(Name, "Toggled greet-me for {0} to {1}", who.Name, toggle);
        }
        #endregion

        #region Event handlers
        void doGreet(Instance bot, Avatar who, bool entering)
        {
            // No greetings within 10 seconds of bot load, to prevent flooding of entries
            // on initial user list load
            if ( VPServices.App.StartUpTime.SecondsToNow() < 10 )
                return;

            var app      = VPServices.App;
            var settings = app.GetUserSettings(who.Name);

            // Do not greet if GreetMe is false
            if ( !settings.GetBoolean(settingGreetMe, true) )
                return;

            foreach ( var target in app.Users )
            {
                // Do not send greet to bots or to the entering user themselves
                if ( target.IsBot || target.Session == who.Session )
                    continue;

                // Only send greet if target wants them
                var targetSettings = app.GetUserSettings(target);
                if ( targetSettings.GetBoolean(settingShowGreets, true) )
                {
                    var msg = entering ? msgEntry : msgExit;
                    bot.ConsoleMessage(target.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msg, who.Name, app.World);
                }
            }
        } 
        #endregion
    }
}
