using System;
using System.Threading;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public DateTime LastConnect;
        public string   World;

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
                    Bot.Login(userName, password);
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
                    Bot.GoTo(0, 10, 0);
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
            Users.Clear();
            ConnectToUniverse();
        }

        void onWorldDisconnect(Instance sender)
        {
            Log.Warn("Network", "Disconnected from world! Reconnecting...");
            Users.Clear();
            ConnectToWorld();
        }
    }
}
