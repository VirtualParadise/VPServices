using System;
using System.Collections.Generic;
using System.Linq;

namespace VPServices
{
    public delegate void WorldEvent(World world);

    class WorldManager
    {
        const string tag = "Worlds";

        public event WorldEvent Added;
        public event WorldEvent Removed;

        List<World> list = new List<World>();

        public void Setup()
        {
            var worlds    = VPServices.Settings.Network.Get("Worlds");
            var worldList = worlds.TerseSplit(',');

            if (worldList.Length == 0)
                throw new InvalidOperationException("No worlds are defined for Services to connect to");

            foreach (var world in worldList)
                Add(world);
        }

        public void Add(string name)
        {
            var world = new World(name);

            Log.Info(tag, "Servicing world '{0}'", name);
            list.Add(world);
            Added(world);
        }

        public void Remove(string name)
        {
            var world = list.FirstOrDefault( w => w.Name.IEquals(name) );

            if (world == null)
                return;

            Log.Info(tag, "No longer servicing world '{0}'", name);
            list.Remove(world);
            Removed(world);
        }
    }
}
