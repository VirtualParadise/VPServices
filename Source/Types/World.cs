using System;
using VP;

namespace VPServices
{
    class World
    {
        const string tag = "World";

        public string   Name;
        public Instance Bot;

        public World(string name)
        {
            Bot = new Instance();

            Log.Fine(tag, "Created bot instance for world '{0}'", name);
        }

        ~World()
        {
            Bot.Dispose();
        }
    }
}
