using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VpNet;
using Nini.Config;

namespace VPServices.Services
{
    /// <summary>
    /// Allows remote kill of services bot by certain users
    /// </summary>
    public class RKill : IService
    {
        public string Name { get { return "RKill"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Services: RKill", "^rkill$", cmdRKill,
                    @"Kills a Services bot by name; restricted to set users",
                    @"!rkill `name`"
                ),
            });

            config = app.Settings.Configs["RKill"] ?? app.Settings.AddConfig("RKill");
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        const string msgDisabled = "RKill is not enabled on this bot";
        const string msgUnauth   = "You are not authorized to remote kill this bot";

        IConfig config;

        bool cmdRKill(VPServices app, Avatar<Vector3> who, string data)
        {
            if (data != app.Bot.Configuration.BotName)
                return true;

            if ( !config.GetBoolean("Enabled", false) )
            {
                app.Warn(who.Session, msgDisabled);
                return true;
            }

            var permitted = config.Get("Users", "");
            if ( !TRegex.IsMatch(who.Name, permitted) )
            {
                app.Warn(who.Session, msgUnauth);
                return true;
            }

            // Perform the kill
            System.Environment.Exit(0);
            return true;
        } 
    }
}
