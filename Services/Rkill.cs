﻿using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VpNet;

namespace VPServices.Services
{
    /// <summary>
    /// Allows remote kill of services bot by certain users
    /// </summary>
    public class RKill : IService
    {
        public string Name { get { return "RKill"; } }
        public void Init(VPServices app, VirtualParadiseClient bot)
        {
            app.Commands.Add(new Command(
                "Services: RKill", "^rkill$", cmdRKill,
                @"Kills a Services bot by name; restricted to set users",
                @"!rkill `name`"
            ));

            config = app.Settings.GetSection("RKill");
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        const string msgDisabled = "RKill is not enabled on this bot";
        const string msgUnauth   = "You are not authorized to remote kill this bot";

        IConfigurationSection config;

        bool cmdRKill(VPServices app, Avatar who, string data)
        {
            if (data != app.Bot.Configuration.BotName)
                return true;

            if (!config.GetValue<bool>("Enabled"))
            {
                app.Warn(who.Session, msgDisabled);
                return true;
            }

            var permitted = config["Users"] ?? "";
            if (!Regex.IsMatch(who.Name, permitted, RegexOptions.IgnoreCase))
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
