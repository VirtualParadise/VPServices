using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    class TeleportHistory : IService
    {
        public string Name { get { return "Teleport history"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "History: Go back", "^(ba?ck|prev)$", cmdDeprecated,
                    @"DEPRECATED; please use VP 0.3.34 for teleport history"
                ),
                
                new Command
                (
                    "History: Go forward", "^(forward|fwd)$", cmdDeprecated,
                    @"DEPRECATED; please use VP 0.3.34 for teleport history"
                ),
            });
        }

        public void Dispose()
        {
        }

        bool cmdDeprecated(VPServices app, Avatar who, string data)
        {
            app.Warn(who.Session, "The !back and !forward commands are no longer in use; please use VP 0.3.34 for teleport history");
            return true;
        }

    }
}
