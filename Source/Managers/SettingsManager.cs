using Nini.Config;
using System;

namespace VPServices
{
    public class SettingsManager
    {
        const string defaultIni  = "Settings.ini";
        const string defaultName = "Services";

        public IConfig Args;
        public IConfig Core;
        public IConfig Network;
        public IConfig Web;

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
            Log.Level = (LogLevels) Enum.Parse( typeof(LogLevels), Args.Get("loglevel", "Production") );
        }

        void setupIni()
        {
            var file = Args.Get("ini", defaultIni);

            ini     = new IniConfigSource(file);
            Core    = ini.Configs.Add("Core");
            Network = ini.Configs.Add("Network");
            Web     = ini.Configs.Add("Web");
        }
    }
}
