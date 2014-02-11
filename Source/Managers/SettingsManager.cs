using Args;
using IniParser;
using System;
using System.Text;
using VPServices.Types;
using IniParser.Model;

namespace VPServices
{
    public class SettingsManager
    {
        const string tag = "Settings";

        public Arguments Args;
        public IniData   Ini;

        public KeyDataCollection Core;
        public KeyDataCollection Network;

        public void Setup(string[] args)
        {
            setupArgs(args);
            Reload();
            Log.Debug(tag, "Global settings and arguments loaded");
        }

        public void Reload()
        {
            Log.Debug(tag, "Using global ini file '{0}'", Args.Ini);
            var parser = new FileIniDataParser();

            Ini     = parser.ReadFile(Args.Ini, Encoding.UTF8);
            Core    = Ini.Sections["Core"];
            Network = Ini.Sections["Network"];
        }

        void setupArgs(string[] args)
        {
            Args = Configuration.Configure<Arguments>().CreateAndBind(args);

            Log.Level = Args.LogLevel;
            Log.Debug(tag, "Logging set up at level {0}", Log.Level);
        }

    }
}
