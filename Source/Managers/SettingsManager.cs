using Args;
using CodeBits;
using System;
using System.Text;
using VPServices.Types;

namespace VPServices
{
    public class SettingsManager
    {
        const string tag         = "Settings";
        const string defaultIni  = "Settings.ini";
        const string defaultName = "Services";

        public Arguments Args;

        public IniFile.Section Core;
        public IniFile.Section Network;
        public IniFile.Section Plugins;

        IniFile ini;

        IniLoadSettings iniLoadSettings = new IniLoadSettings()
        {
            CaseSensitive = false,
            Encoding      = Encoding.UTF8
        };

        public void Setup(string[] args)
        {
            setupArgs(args);
            Reload();
            Log.Debug(tag, "Global settings and arguments loaded");
        }

        public void Reload()
        {
            Log.Debug(tag, "Using global ini file '{0}'", Args.Ini);
            ini = new IniFile(Args.Ini, iniLoadSettings);

            Core    = ini["Core"];
            Network = ini["Network"];
            Plugins = ini["Plugins"];
        }

        void setupArgs(string[] args)
        {
            Args = Configuration.Configure<Arguments>().CreateAndBind(args);

            Log.Level = Args.LogLevel;
            Log.Debug(tag, "Logging set up at level {0}", Log.Level);
        }

    }
}
