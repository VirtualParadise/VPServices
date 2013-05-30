using Nini.Config;
using System;
using System.IO;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        const string defaultFileSettings = "Settings.ini";
        const string defaultName         = "Services";

        /// <summary>
        /// Bot settings INI
        /// </summary>
        public IniConfigSource Settings = new IniConfigSource { AutoSave = true };

        public IConfig CoreSettings;
        public IConfig NetworkSettings;
        public IConfig WebSettings;

        public void SetupSettings(string[] args)
        {
            var argConfig = new ArgvConfigSource(args);
            argConfig.AddSwitch("Args", "ini", "i");

            var file = argConfig.Configs["Args"].Get("ini", defaultFileSettings);

            if ( File.Exists(file) )
            {
                Settings.Load(file);
                CoreSettings    = Settings.Configs["Core"];
                NetworkSettings = Settings.Configs["Network"];
                WebSettings     = Settings.Configs["Web"];
            }
            else
            {
                CoreSettings    = Settings.Configs.Add("Core");
                NetworkSettings = Settings.Configs.Add("Network");
                WebSettings     = Settings.Configs.Add("Web");
            }
        }
    }
}
