using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static VPServices App;
        public static Random     Rand = new Random();

        public static Color ColorLesser = new Color(150,150,200);
        public static Color ColorInfo   = new Color(50,50,100);
        public static Color ColorWarn   = new Color(220,80,20);
        public static Color ColorAlert  = new Color(255,0,0);

        public Instance Bot;
        public string   Owner;
        public bool     Crash;
        readonly ILogger servicesLogger;

        public static async Task<int> Main(string[] args)
        {
            int exit = 0;
            
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);

            try
            {
                App = new VPServices(args);
                await App.Setup();

                while (true)
                    await App.UpdateLoop();
            }
            catch (Exception e)
            {
                Log.ForContext("Tag", "Services").Fatal(e, "Unhandled error");
                exit = 1;
            }
            finally
            {
                App.Dispose();
            }

            Console.WriteLine("### [{0}] Services is now exiting", DateTime.Now);
            return exit;
        }

        public VPServices(string[] args)
        {
            SetupSettings(args);
            SetupLogging();
            servicesLogger = Log.ForContext("Tag", "Services");
            commandsLogger = Log.ForContext("Tag", "Commands");
            webServerLogger = Log.ForContext("Tag", "Web server");
            networkLogger = Log.ForContext("Tag", "Network");
        }

        private void SetupLogging()
        {
            var logLevel = CoreSettings.GetValue<LogEventLevel>("LogLevel", LogEventLevel.Information);
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Tag}] {Message}{NewLine}{Exception}");

            Log.Logger = loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Sets up bot instance
        /// </summary>
        public async Task Setup()
        {
            botName = CoreSettings.GetValue("Name", defaultName);
            userName = NetworkSettings.GetValue("Username", "");
            password = NetworkSettings.GetValue("Password", "");
            World = NetworkSettings.GetValue("World", "");
            Owner = userName;

            Bot = new Instance();

            // Connect to network
            await ConnectToUniverse();
            Log.ForContext("Tag", "Network").Information("Connected to universe");

            // Set up subsystems
            SetupDatabase();
            SetupWeb();
            SetupCommands();
            SetupEvents();
            LoadServices();

            // Set up services
            await ConnectToWorld();
            PerformMigrations();
            InitServices();
            Log.ForContext("Tag", "Network").Information("Connected to {World}", World);
            
            SaveMigrationVersion(MigrationVersion);
            var result = Bot.ConsoleMessage("", "Services is now online; say !help for information", ColorInfo);
        }

        /// <summary>
        /// Pumps bot events
        /// </summary>
        public async Task UpdateLoop()
        {
            if (Crash)
            {
                Crash = false;
                throw new Exception("Forced crash in update loop");
            }
            
            await Task.Delay(100);
        }

        /// <summary>
        /// Disposes of application by clearing all loaded service and disposing of bot
        /// </summary>
        public void Dispose()
        {
            Commands.Clear();

            ClearWeb();
            ClearEvents();
            ClearServices();
            CloseDatabase();
            Bot.Dispose();
        }

        #region Helper functions
        public static bool TryParseBool(string msg, out bool value)
        {
            if ( Regex.IsMatch(msg, "^(true|1|yes|on)$", RegexOptions.IgnoreCase) )
            {
                value = true;
                return true;
            }
            else if ( Regex.IsMatch(msg, "^(false|0|no|off)$", RegexOptions.IgnoreCase) )
            {
                value = false;
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }

        public void Notify(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, Bot.Configuration.BotName, string.Format(msg, parts), ColorInfo, TextEffectTypes.Italic);
        }

        public void NotifyAll(string msg, params object[] parts)
        {
            Notify(0, msg, parts);
        }

        public void Alert(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, Bot.Configuration.BotName, string.Format(msg, parts), ColorAlert, TextEffectTypes.Bold);
        }

        public void AlertAll(string msg, params object[] parts)
        {
            Alert(0, msg, parts);
        }

        public void Warn(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, Bot.Configuration.BotName, string.Format(msg, parts), ColorWarn, TextEffectTypes.Italic);
        }

        public void WarnAll(string msg, params object[] parts)
        {
            Warn(0, msg, parts);
        }
        #endregion
    }
}
