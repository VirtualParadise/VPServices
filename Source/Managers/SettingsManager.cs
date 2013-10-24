using Nini.Config;
using System;
using System.IO;

namespace VPServices
{
    public class SettingsManager
    {
        const string tag         = "Settings";
        const string defaultIni  = "Settings.ini";
        const string defaultName = "Services";

        public IConfig Args;
        public IConfig Core;
        public IConfig Network;
        public IConfig Plugins;

        IniConfigSource ini;

        public void Setup(string[] args)
        {
            setupArgs(args);
            setupIni();
            Log.Debug(tag, "Global settings and arguments loaded");
        }

        public void Reload()
        {
            ini.Reload();
        }

        void setupArgs(string[] args)
        {
            var source = new ArgvConfigSource(args);

            source.AddSwitch("Args", "ini",      "i");
            source.AddSwitch("Args", "loglevel", "l");

            Args      = source.Configs["Args"];
            Log.Level = TEnums.Parse<LogLevels>( Args.Get("loglevel", "Production") );
            Log.Debug(tag, "Logging set up at level {0}", Log.Level);
        }

        void setupIni()
        {
            var file = Args.Get("ini", defaultIni);
            Log.Info(tag, "Using global ini file '{0}'", file);

            ini        = new IniConfigSource(file);
            Core       = ini.Configs["Core"]    ?? ini.Configs.Add("Core");
            Network    = ini.Configs["Network"] ?? ini.Configs.Add("Network");
            Plugins    = ini.Configs["Plugins"] ?? ini.Configs.Add("Plugins");

            if ( !File.Exists(file) )
                throw new NotImplementedException("TODO: Create ini and exit");
        }
    }
}
