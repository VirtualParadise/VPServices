using System;
using System.Collections.Generic;
using System.Linq;

namespace VPServices.Internal
{
    public class UserMessages
    {
        User user;

        public UserMessages(User user)
        {
            this.user = user;
        }

        public void Lesser(string msg, params object[] parts)
        {
            VPServices.Messages.Send(user, Colors.Lesser, msg, parts);
        }

        public void Info(string msg, params object[] parts)
        {
            VPServices.Messages.Send(user, Colors.Info, msg, parts);
        }

        public void Warn(string msg, params object[] parts)
        {
            VPServices.Messages.Send(user, Colors.Warn, msg, parts);
        }

        public void Alert(string msg, params object[] parts)
        {
            VPServices.Messages.Send(user, Colors.Alert, msg, parts);
        }
    }
}
