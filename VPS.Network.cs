using System;
using VP;

namespace VPServ
{
    public partial class VPServ : IDisposable
    {
        public DateTime StartUpTime;
        public string World;

        string userName;
        string password;
        short connAttempts;

        /// <summary>
        /// Makes up to 10 connection attempts to the universe
        /// </summary>
        void ConnectToUniverse()
        {
            connAttempts = 1;
            while (connAttempts <= 10)
            {
                try
                {
                    Bot.Login(userName, password);

                    // Disconnect events
                    Bot.WorldDisconnect += onWorldDisconnect;
                    Bot.UniverseDisconnect += onUniverseDisconnect;
                    return;
                }
                catch (Exception e)
                {
                    Log.Warn("Network", "Failed to connect to universe: {0} ({1} / 10)", e.Message, connAttempts);
                    connAttempts++;
                }
            }

            throw new Exception("Could not connect to uniserver after ten attempts.");
        }

        /// <summary>
        /// Makes up to 10 connection attempts to the world
        /// </summary>
        void ConnectToWorld()
        {
            connAttempts = 1;
            while (connAttempts <= 10)
            {
                try
                {
                    Bot.Enter(World);
                    Bot.GoTo(0, 10, 0);
                    StartUpTime = DateTime.Now;
                    return;
                }
                catch (Exception e)
                {
                    Log.Warn("Network", "Failed to connect to world: {0} ({1} / 10)", e.Message, connAttempts);
                    connAttempts++;
                }
            }

            throw new Exception("Could not connect to worldserver after ten attempts.");
        }

        void onUniverseDisconnect(Instance sender)
        {
            Log.Warn("Network", "Disconnected from universe! Reconnecting...");
            ConnectToUniverse();
        }

        void onWorldDisconnect(Instance sender)
        {
            Log.Warn("Network", "Disconnected from world! Reconnecting...");
            ConnectToWorld();
        }
    }
}
