using System;
using System.Threading.Tasks;
using VP;

namespace VPServices
{
    public class World : IDisposable
    {
        const string tag = "World";

        public string     Name;
        public bool       Enabled     = true;
        public Instance   Bot;
        public DateTime   LastAttempt = TDateTime.UnixEpoch;
        public DateTime   LastConnect = TDateTime.UnixEpoch;
        public WorldState State       = WorldState.Disconnected;

        public World(string name)
        {
            Name = name;
            Bot  = new Instance();
            Bot.UniverseDisconnect += onDisconnect;
            Bot.WorldDisconnect    += onDisconnect;

            Log.Fine(tag, "Created bot instance for world '{0}'", name);
        }

        public async void Connect()
        {
            State = WorldState.Connecting;

            await Task.Run( () => {
                var username = VPServices.Settings.Network.Get("Username");
                var password = VPServices.Settings.Network.Get("Password");
                var botname  = VPServices.Settings.Network.Get("Name");
                LastAttempt  = DateTime.Now;

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

                    State = WorldState.Disconnected;
                    return;
                }

                Log.Fine(tag, "Connected to '{0}'", Name);
                LastConnect = DateTime.Now;
                State       = WorldState.Connected;
            });
        }

        void onDisconnect(Instance sender, int error)
        {
            State = WorldState.Disconnected;
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
