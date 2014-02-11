using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices.Internal
{
    public delegate void ServicesWorldArgs(World world);

    public class WorldManager
    {
        const string tag = "Worlds";

        public event ServicesWorldArgs Added;
        public event ServicesWorldArgs Removed;

        List<World> worlds = new List<World>();

        bool worldsModified = false;

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

        /// <summary>
        /// Adds a world for Services to service
        /// </summary>
        /// <param name="name">Case-sensitive Virtual Paradise name of the world</param>
        /// <returns>True if added, false if already added</returns>
        public bool Add(string name)
        {
            if ( Get(name) != null)
                return false;

            var world = new World(name);

            Log.Info(tag, "Servicing world '{0}'", name);
            worlds.Add(world);
            Added(world);

            worldsModified = true;
            return true;
        }

        /// <summary>
        /// Removes a world from Services, disposing of its bot instance
        /// </summary>
        /// <param name="name">Case-sensitive Virtual Paradise name of the world</param>
        /// <returns>True if removed, false if did not exist</returns>
        public bool Remove(string name)
        {
            var world = Get(name);

            if (world == null)
                return false;

            Log.Info(tag, "No longer servicing world '{0}'", name);
            worlds.Remove(world);
            Removed(world);
            world.Dispose();

            worldsModified = true;
            return true;
        }

        /// <summary>
        /// Gets the <see cref="World"/> instance that uses the given
        /// <see cref="Instance"/>
        /// </summary>
        /// <param name="bot">Virtual Paradise bot instance</param>
        /// <returns>Attached world if exists, null if not</returns>
        public World Get(Instance bot)
        {
            return worlds.FirstOrDefault(w => w.Bot == bot);
        }

        /// <summary>
        /// Gets the <see cref="World"/> instance by case-sensitive name
        /// </summary>
        /// <param name="name">Case-sensitive Virtual Paradise name of the world</param>
        /// <returns>World if serviced, null if not</returns>
        public World Get(string name)
        {
            return worlds.FirstOrDefault( w => w.Name.Equals(name) );
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

                // Nessecary if worlds are added or removed by commands
                if (worldsModified)
                {
                    worldsModified = false;
                    break;
                }
            }
        }
    }
}
