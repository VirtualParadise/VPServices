using System;
using System.Threading.Tasks;
using VP;

namespace VPServices
{
    public class World : IDisposable
    {
        const string tag = "World";

        public readonly Instance Bot = new Instance();
        public readonly string   Name;

        public bool Enabled = true;

        WorldState state = WorldState.Disconnected;
        /// <summary>
        /// Gets the connection state of this world
        /// </summary>
        public WorldState State
        {
            get { return state; }
        }

        DateTime lastAttempt = TDateTime.UnixEpoch;
        /// <summary>
        /// Gets the timestamp of the last connection attempt of this world
        /// </summary>
        public DateTime LastAttempt
        {
            get { return lastAttempt; }
        }

        DateTime lastConnect = TDateTime.UnixEpoch;
        /// <summary>
        /// Gets the timestamp of the last successful connection to this world
        /// </summary>
        public DateTime LastConnect
        {
            get { return lastConnect; }
        }

        public World(string name)
        {
            Name = name;
            Bot.UniverseDisconnect += onDisconnect;
            Bot.WorldDisconnect    += onDisconnect;

            Log.Fine(tag, "Created bot instance for world '{0}'", name);
        }

        public async void Connect()
        {
            state = WorldState.Connecting;

            await Task.Run( () => {
                var username = VPServices.Settings.Network.Get("Username");
                var password = VPServices.Settings.Network.Get("Password");
                var botname  = VPServices.Settings.Network.Get("Name");
                lastAttempt  = DateTime.Now;

                try
                {
                    Bot.Login(username, password, botname)
                        .Enter(Name)
                        .Pump();
                }
                catch (VPException e)
                {
                    switch (e.Reason)
                    {
                        case ReasonCode.WorldNotFound:
                            Log.Warn(tag, "World '{0}' does not exist, will disable", Name);
                            Enabled = false;
                            break;

                        default:
                            Log.Warn(tag, "World '{0}' cannot connect: {1}", Name, e.Reason);
                            break;
                    }

                    state = WorldState.Disconnected;
                    return;
                }

                Log.Debug(tag, "Connected to '{0}'", Name);
                lastConnect = DateTime.Now;
                state       = WorldState.Connected;
            });
        }

        void onDisconnect(Instance sender, int error)
        {
            Log.Warn(tag, "Lost connection to '{0}', winsock error {1}", Name, error);
            state = WorldState.Disconnected;
        }

        public void Dispose()
        {
            Bot.Dispose();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum WorldState
    {
        Disconnected,
        Connecting,
        Connected
    }
}
