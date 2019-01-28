using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using System;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        const string defaultFileSettings = "Settings.ini";
        const string defaultName         = "Services";

        /// <summary>
        /// Bot settings INI
        /// </summary>
        public IConfigurationRoot Settings;

        public IConfigurationSection CoreSettings;
        public IConfigurationSection NetworkSettings;
        public IConfigurationSection WebSettings;

        public void SetupSettings(string[] args)
        {
            var argConfig = new CommandLineConfigurationProvider(args);
            argConfig.Load();

            string file;
            if (!argConfig.TryGet("ini", out file))
            {
                file = defaultFileSettings;
            }

            Settings = new ConfigurationBuilder()
                .AddIniFile(file)
                .Build();

            CoreSettings = Settings.GetSection("Core");
            NetworkSettings = Settings.GetSection("Network");
            WebSettings = Settings.GetSection("Web");
        }
    }
}
