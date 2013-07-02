using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Stores and log world settings
    /// </summary>
    class WorldSettings : ServiceBase
    {
        public Dictionary<string, string> Data = new Dictionary<string, string>();

        public string Name { get { return "World Settings"; } }
        public void   Load (VPServices app, Instance bot)
        {
            bot.Data.GetWorldSetting += onWorldSetting;

            app.Routes.Add(new WebRoute("WorldSettings", "^worldsettings?$", webWorldSettings,
                @"Provides a key-value list of the settings of the bot's world"));
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose()
        {
            Data.Clear();
        }

        void onWorldSetting(Instance sender, string key, string value)
        {
            Log.Fine(Name, "Retrieved world setting: {0} : {1}", key, value);
            Data[key] = value;
        }

        string webWorldSettings(VPServices app, string data)
        {
            string listing = string.Format("# Settings for world '{0}':\n", app.World);

            foreach (var setting in Data)
            {
                if ( setting.Key.Equals("objectpassword", StringComparison.CurrentCultureIgnoreCase) )
                    listing += "* *Object password hidden*\n";
                else
                    listing += string.Format("* **{0}** : {1}\n", setting.Key, setting.Value);
            }

            return app.MarkdownParser.Transform(listing);
        }
    }
}
