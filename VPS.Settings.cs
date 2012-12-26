using Nini.Config;
using System;
using System.IO;

namespace VPServ
{
    public partial class VPServ : IDisposable
    {
        public const string FILE_SETTINGS = "Settings.ini";

        /// <summary>
        /// Bot settings INI
        /// </summary>
        public IniConfigSource Settings = new IniConfigSource { AutoSave = true };

        public IConfig NetworkSettings;
        public IConfig WebSettings;

        public void SetupSettings()
        {
            if (File.Exists(FILE_SETTINGS))
            {
                Settings.Load(FILE_SETTINGS);
                NetworkSettings = Settings.Configs["Network"];
                WebSettings = Settings.Configs["Web"];
            }
            else
            {
                NetworkSettings = Settings.Configs.Add("Network");
                WebSettings = Settings.Configs.Add("Web");
                Settings.Save(FILE_SETTINGS);
            }
        }
    }
}
