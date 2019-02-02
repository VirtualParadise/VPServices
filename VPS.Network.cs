using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public DateTime LastConnect;
        public string   World;

        string botName;
        string userName;
        string password;
        readonly ILogger networkLogger;

        /// <summary>
        /// Makes up to 10 connection attempts to the universe
        /// </summary>
        async Task ConnectToUniverse()
        {
            while (true)
            {
                try
                {
                    await Bot.ConnectAsync();
                    await Bot.LoginAsync(userName, password, botName);
                    LastConnect = DateTime.Now;
                    
                    // Disconnect events
                    Bot.OnWorldDisconnect    += onWorldDisconnect;
                    Bot.OnUniverseDisconnect += onUniverseDisconnect;
                    return;
                }
                catch (Exception e)
                {
                    networkLogger.Warning(e, "Failed to connect to universe: {Error}", e.Message);
                    Thread.Sleep(30000);
                }
            }

            throw new Exception("Could not connect to uniserver after ten attempts.");
        }

        /// <summary>
        /// Makes up to 10 connection attempts to the world
        /// </summary>
        async Task ConnectToWorld()
        {
            while (true)
            {
                try
                {
                    await Bot.EnterAsync(World);
                    Bot.UpdateAvatar(new Vector3(0, 0, 0));
                    LastConnect = DateTime.Now;
                    return;
                }
                catch (Exception e)
                {
                    networkLogger.Warning(e, "Failed to connect to world: {Error}", e.Message);
                    Thread.Sleep(30000);
                }
            }

            throw new Exception("Could not connect to worldserver after ten attempts.");
        }

        async void onUniverseDisconnect(Instance sender, UniverseDisconnectEventArgs args)
        {
            networkLogger.Warning("Disconnected from universe! Reconnecting...");

            lock (SyncMutex)
                Users.Clear();

            await ConnectToUniverse();
        }

        async void onWorldDisconnect(Instance sender, WorldDisconnectEventArgs args)
        {
            networkLogger.Warning("Disconnected from world! Reconnecting...");

            lock (SyncMutex)
                Users.Clear();

            await ConnectToWorld();
        }
    }
}
