using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices
{
    public delegate void ServicesWorldArgs(World world);

    public class WorldManager
    {
        const string tag = "Worlds";

        public event ServicesWorldArgs Added;
        public event ServicesWorldArgs Removed;

        List<World> worlds = new List<World>();

        public void Setup()
        {
            var worlds    = VPServices.Settings.Network["Worlds"] ?? "VP-Build";
            var worldList = worlds.TerseSplit(',');

            if (worldList.Length == 0)
                throw new InvalidOperationException("No worlds are defined for Services to connect to");

            foreach (var world in worldList)
                Add(world);
        }

        public void Takedown()
        {
            foreach (var world in worlds)
            {
                Removed(world);
                world.Dispose();
            }

            Added   = null;
            Removed = null;

            worlds.Clear();
            Log.Info(tag, "All worlds de-registered");
        }

        public void Add(string name)
        {
            var world = new World(name);

            Log.Info(tag, "Servicing world '{0}'", name);
            worlds.Add(world);
            Added(world);
        }

        public void Remove(string name)
        {
            var world = worlds.FirstOrDefault( w => w.Name.IEquals(name) );

            if (world == null)
                return;

            Log.Info(tag, "No longer servicing world '{0}'", name);
            world.Dispose();
            worlds.Remove(world);
            Removed(world);
        }

        public World Get(Instance bot)
        {
            return worlds.FirstOrDefault(w => w.Bot == bot);
        }

        public World[] GetAll()
        {
            return worlds.ToArray();
        }

        public void Update()
        {
            foreach (var world in worlds)
            {
                if (!world.Enabled || world.State == WorldState.Connecting)
                    continue;

                if (world.State == WorldState.Disconnected && world.LastAttempt.SecondsToNow() > 20)
                {
                    Log.Debug(tag, "World '{0}' is not connected; connecting...", world);
                    world.Connect();

                    continue;
                }

                if (world.State == WorldState.Connected)
                    world.Bot.Pump();
            }
        }
    }
}
