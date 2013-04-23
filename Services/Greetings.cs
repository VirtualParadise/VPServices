using Nini.Config;
using System;
using System.Linq;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Handles user entry / exit announcements
    /// </summary>
    public class Greetings : IService
    {
        const  string settingGreetMe    = "GreetMe";
        const  string settingShowGreets = "GreetShow";
        const  string msgEntry          = "*** {0} has entered";
        const  string msgExit           = "*** {0} has left";
        const  string msgShowGreets     = "Entry/exit messages will now be shown";
        const  string msgHideGreets     = "Entry/exit messages will no longer be shown";
        const  string msgGreetMe        = "You will now be announced on entry/exit";
        const  string msgGreetMeNot     = "You will no longer be announced on entry/exit";
        static Color  colorGreet        = VPServices.ColorInfo;

        public string Name { get { return "Greetings"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command("Greets show/hide", "^greet(ing)?s?$", cmdToggleGreets,
                @"Toggles whether or not the bot sends you user entry/exit messages"),

                new Command("Greets stealth", "^greetme$", cmdToggleGreetMe,
                @"Toggles whether or not the bot should announce your entry and exit to other users"),
            });

            bot.Avatars.Enter += (b,a) => { doGreet(b, a, true);  };
            bot.Avatars.Leave += (b,a) => { doGreet(b, a, false); };
        }

        public void Dispose() { }

        void doGreet(Instance bot, Avatar who, bool entering)
        {
            // No greetings within 10 seconds of bot load
            if (VPServices.App.StartUpTime.SecondsToNow() < 10)
                return;

            var app      = VPServices.App;
            var settings = app.GetUserSettings(who.Name);

            // Do not greet if GreetMe is false
            if ( !settings.GetBoolean(settingGreetMe, true) )
                return;
            
            foreach (var target in app.Users)
            {
                if (target.IsBot || target.Session == who.Session)
                    continue;

                var targetSettings = app.GetUserSettings(target);
                if ( targetSettings.GetBoolean(settingShowGreets, true) )
                {
                    var msg = entering ? msgEntry : msgExit;
                    bot.ConsoleMessage(target.Session, ChatTextEffect.Italic, colorGreet, "", msg, who.Name);
                }
            }
        }

        void cmdToggleGreets(VPServices app, Avatar who, string data)
        {
            var config = app.GetUserSettings(who);
            var toggle = !config.GetBoolean(settingShowGreets, true);
            var msg    = toggle ? msgShowGreets : msgHideGreets;
            config.Set(settingShowGreets, toggle);

            app.Bot.ConsoleMessage(who.Session, ChatTextEffect.Italic, colorGreet, "", msg);
            Log.Debug(Name, "Toggled greetings messages for {0} to {1}", who.Name, toggle);
        }

        void cmdToggleGreetMe(VPServices app, Avatar who, string data)
        {
            var config = app.GetUserSettings(who);
            var toggle = !config.GetBoolean(settingGreetMe, true);
            var msg    = toggle ? msgGreetMe : msgGreetMeNot;
            config.Set(settingGreetMe, toggle);

            app.Bot.ConsoleMessage(who.Session, ChatTextEffect.Italic, colorGreet, "", msg);
            Log.Debug(Name, "Toggled greet-me for {0} to {1}", who.Name, toggle);
        }
    }
}
