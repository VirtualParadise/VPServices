using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPServices
{
    class WorldManager
    {
        public event Action<World> Added;
        public event Action<World> Removed;

        Dictionary<string, World> list = new Dictionary<string,World>();
    }
}
