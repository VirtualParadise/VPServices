﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using VP;

namespace VPServices
{
    partial class VPServices
    {
        /// <summary>
        /// Build monitor
        /// </summary>
        public static StreamWriter BuildMon = new StreamWriter("BuildHist.dat", true)
        {
            AutoFlush = true
        };

        /// <summary>
        /// Build monitor
        /// </summary>
        public static StreamWriter UserMon = new StreamWriter("UserHist.dat", true)
        {
            AutoFlush = true
        };

        public static Instance Bot = new Instance();

        static string userName;
        static string password;
        static string world;
        static DateTime lastHelp = DateTime.MinValue;

        public static Services.UserManager UserManager = new Services.UserManager();
        public static Services.Telegrams Telegrams = new Services.Telegrams();
        public static Services.Jumps Jumps = new Services.Jumps();

        static void Main(string[] args)
        {
            // Load settings
            if (args.Length != 3)
            {
                Console.WriteLine(@"Services; to run: vpservices.exe ""username"" ""password"" ""world"" ");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                goto exit;
            }

            try
            {
                userName = args[0];
                password = args[1];
                world = args[2];

                // Connect to world
                Console.WriteLine("Connecting to universe...");
                ConnectToUniverse();

                // Set up global events
                Console.WriteLine("Connected to world.");
                Bot.Comms.Chat += OnChat;
                Bot.World.Disconnect += onWorldDisconnect;
                Bot.Universe.Disconnect += onUniverseDisconnect;
                Bot.Property.ObjectCreate += OnObjChange;
                Bot.Property.ObjectChange += OnObjChange;
                while (true) { UpdateLoop(); }
            }
            catch (Exception e) {
                var ex = e;
                while (true)
                {
                    Console.WriteLine("Exception: {0}, {1}", ex.Message, ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                        continue;
                    }
                    else break;
                }
                goto exit;
            }

        exit:
            BuildMon.Flush();
            BuildMon.Close();
            return;
        }

        static void UpdateLoop()
        {
            Bot.Wait(-1);
            Thread.Sleep(1000);
        }

        static void OnObjChange(Instance sender, int sessionId, VPObject o)
        {
            BuildMon.WriteLine("{0},{1},{2},{3}",
                Math.Round(o.Position.X, 3),
                Math.Round(o.Position.Y, 2),
                Math.Round(o.Position.Z, 3),
                (int) DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
                );
        }

        static void OnChat(Instance sender, Chat chat)
        {
            if (!chat.Message.StartsWith("!")) return;

            var intercept = chat.Message
                .Trim()
                .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            var requester = UserManager[chat.Session].Avatar;
            var command = intercept[0].Substring(1).ToLower();
            var data = intercept.Length == 2
                ? intercept[1].Trim()
                : "";

            switch (command)
            {
                case "telegram":
                    Telegrams.OnCommand(sender, chat, data);
                    return;
                case "blocktelegrams":
                    Telegrams.Block(sender, chat.Name.ToLower());
                    return;
                case "help":
                case "commands":
                    if (DateTime.Now.Subtract(lastHelp).TotalSeconds < 60) return;
                    Bot.Comms.Say("!telegram <who>: <message> , !blocktelegrams , !seed, !mycoords, !addjump <name>, !deljump <name>, !j(ump) <name>");
                    lastHelp = DateTime.Now;
                    return;
                case "seed":
                    Bot.World.UpdateAvatar(requester.X, requester.Y, requester.Z);
                    Bot.Comms.Say("At your location; right click me to duplicate my avatar into a new object");
                    return;
                case "mycoords":
                    Bot.Comms.Say("{0}: {1}, {2}, {3}", requester.Name, requester.X, requester.Y, requester.Z);
                    return;
                case "addjump":
                    Jumps.CmdAddJump(sender, requester, data);
                    return;
                case "deljump":
                    Jumps.CmdDelJump(sender, requester, data);
                    return;
                case "jump":
                case "j":
                    Jumps.CmdJump(sender, requester, data);
                    return;
                case "back":
                    UserManager.CmdGoBack(sender, requester, data);
                    return;
                case "crash":
                    if (requester.Name != "Roy Curtis") return;
                    throw new Exception("Forced crash");
                default:
                    Console.WriteLine("Unknown command: {0}", command);
                    return;
            }

        }

        
    }
}
