using System;
using System.Collections.Generic;
using System.Linq;

namespace VPServices.Internal
{
    public class WorldMessages
    {
        World world;

        public WorldMessages(World world)
        {
            this.world = world;
        }

        public void Lesser(string msg, params object[] parts)
        {
            VPServices.Messages.Broadcast(world, Colors.Lesser, msg, parts);
        }

        public void Info(string msg, params object[] parts)
        {
            VPServices.Messages.Broadcast(world, Colors.Info, msg, parts);
        }

        public void Warn(string msg, params object[] parts)
        {
            VPServices.Messages.Broadcast(world, Colors.Warn, msg, parts);
        }

        public void Alert(string msg, params object[] parts)
        {
            VPServices.Messages.Broadcast(world, Colors.Alert, msg, parts);
        }
    }
}
