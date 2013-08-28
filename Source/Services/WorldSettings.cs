using System;
using System.Linq;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Stores and log world settings
    /// </summary>
    class WorldSettings : IService
    {
        public Dictionary<string, string> Data = new Dictionary<string, string>();

        public string Name
        { 
            get { return "World Settings"; }
        }

        public void Load (VPServices app, Instance bot)
        {
            bot.Data.GetWorldSetting += onWorldSetting;

            app.Commands.AddRange(new[] {
                new Command
                (
                    "World: Settings", @"^world(settings|attributes)$", cmdListSettings,
                    @"Lists all of this world's settings or searches keys and values matching given term",
                    @"!worldsettings `[search term]`"
                ),
            });
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose()
        {
            Data.Clear();
        }

        const string msgTitle    = "*** World settings for '{0}'";
        const string msgResults  = "*** Search results for '{0}'";
        const string msgResult   = "{0} : {1}";
        const string errNotFound = "Could not match any world setting for '{0}'; try `!worldsettings`";

        void onWorldSetting(Instance sender, string key, string value)
        {
            Log.Fine(Name, "Retrieved world setting: {0} : {1}", key, value);
            Data[key] = value;
        }

        bool cmdListSettings(VPServices app, Avatar who, string data)
        {
            if (data != "")
            {
                var query = from    s in Data
                            where  (s.Key + s.Value).Contains(data)
                            select  s;

                if (query.Count() == 0)
                    app.Warn(who.Session, errNotFound, data);
                else
                {
                    app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgResults, data);

                    foreach (var setting in query)
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult, setting.Key.PadRight(10), setting.Value);
                }
            }
            else
            {
                app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgTitle, app.World);

                foreach (var setting in Data)
                    app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult, setting.Key.PadRight(10), setting.Value);
            }

            return true;
        }
    }
}
