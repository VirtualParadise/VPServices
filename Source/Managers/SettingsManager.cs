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

        IniConfigSource ini;

        public void Setup(string[] args)
        {
            setupArgs(args);
            setupIni();
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
            Log.Level = TEnums.Parse<LogLevels>( Args.Get("loglevel", "All") );
            Log.Debug(tag, "Logging set up at level {0}", Log.Level);
        }

        void setupIni()
        {
            var file = Args.Get("ini", defaultIni);
            Log.Info(tag, "Using global ini file '{0}'", file);

            ini     = new IniConfigSource(file);
            Core    = ini.Configs.Add("Core");
            Network = ini.Configs.Add("Network");

            if ( !File.Exists(file) )
                throw new NotImplementedException("TODO: Create ini and exit");
        }
    }
}
