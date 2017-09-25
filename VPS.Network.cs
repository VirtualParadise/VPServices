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

                    Bot.Login(userName, password, botName);
                    LastConnect = DateTime.Now;
                    
                    // Disconnect events
                    Bot.WorldDisconnect    += onWorldDisconnect;
                    Bot.UniverseDisconnect += onUniverseDisconnect;
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
                    Bot.Enter(World);
                    Avatar<Vector3> myAvatar = Bot.My();
                    Bot.TeleportAvatar(myAvatar, new Vector3(0, 10, 0), new Vector3());
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

        void onUniverseDisconnect(Instance sender)
        {
            Log.Warn("Network", "Disconnected from universe! Reconnecting...");

            lock (SyncMutex)
                Users.Clear();

            ConnectToUniverse();
        }

        void onWorldDisconnect(Instance sender)
        {
            Log.Warn("Network", "Disconnected from world! Reconnecting...");

            lock (SyncMutex)
                Users.Clear();

            ConnectToWorld();
        }
    }
}
