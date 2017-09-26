using System;
using System.Threading;
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

        /// <summary>
        /// Makes up to 10 connection attempts to the universe
        /// </summary>
        void ConnectToUniverse()
        {
            while (true)
            {
                try
                {
                    // TODO: Async. Example:
                    // vp.ConnectAsync();
                    // vp.LoginAsync(user, password, botname);
                    // vp.EnterAsync(world);

                    var connect = Bot.ConnectAsync().Result;
                    var login = Bot.LoginAsync(userName, password, botName).Result;
                    LastConnect = DateTime.Now;
                    
                    // Disconnect events
                    Bot.OnWorldDisconnect    += onWorldDisconnect;
                    Bot.OnUniverseDisconnect += onUniverseDisconnect;
                    return;
                }
                catch (Exception e)
                {
                    Log.Warn("Network", "Failed to connect to universe: {0}", e.Message);
                    Thread.Sleep(30000);
                }
            }

            throw new Exception("Could not connect to uniserver after ten attempts.");
        }

        /// <summary>
        /// Makes up to 10 connection attempts to the world
        /// </summary>
        void ConnectToWorld()
        {
            while (true)
            {
                try
                {
                    var enter = Bot.EnterAsync(World).Result;
                    Avatar<Vector3> myAvatar = Bot.My();
                    Bot.Say("Hey!");
                    Bot.UpdateAvatar(new Vector3(0, 0, 0));
                    LastConnect = DateTime.Now;
                    return;
                }
                catch (Exception e)
                {
                    Log.Warn("Network", "Failed to connect to world: {0}", e.Message);
                    Thread.Sleep(30000);
                }
            }

            throw new Exception("Could not connect to worldserver after ten attempts.");
        }

        void onUniverseDisconnect(Instance sender, UniverseDisconnectEventArgs args)
        {
            Log.Warn("Network", "Disconnected from universe! Reconnecting...");

            lock (SyncMutex)
                Users.Clear();

            ConnectToUniverse();
        }

        void onWorldDisconnect(Instance sender, WorldDisconnectEventArgs args)
        {
            Log.Warn("Network", "Disconnected from world! Reconnecting...");

            lock (SyncMutex)
                Users.Clear();

            ConnectToWorld();
        }
    }
}
