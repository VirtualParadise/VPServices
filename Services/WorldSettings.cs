using System;
using System.Collections.Generic;
using VP;

namespace VPServ.Services
{
    /// <summary>
    /// Stores and log world settings
    /// </summary>
    class WorldSettings : IService
    {
        public Dictionary<string, string> Data = new Dictionary<string, string>();

        public string Name { get { return "World Settings"; } }

        public void Init(VPServ app, Instance bot)
        {
            bot.Data.GetWorldSetting += onWorldSetting;
        }

        public void Dispose()
        {
            Data.Clear();
        }

        void onWorldSetting(Instance sender, string key, string value)
        {
            Log.Fine(Name, "Retrieved world setting: {0} : {1}", key, value);
            Data[key] = value;
        }
    }
}
