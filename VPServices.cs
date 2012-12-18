using System;
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

        public static Instance Bot = new Instance();
        public static Random Rand = new Random();
        public static DateTime StartUpTime;

        static string userName;
        static string password;
        static string world;
        static DateTime lastHelp = DateTime.MinValue;

        public static Services.UserManager UserManager = new Services.UserManager();
        public static Services.Telegrams Telegrams = new Services.Telegrams();
        public static Services.Jumps Jumps = new Services.Jumps();
        public static Services.JoinsInvites JoinsInvites = new Services.JoinsInvites();
        public static Services.KickBans KickBans = new Services.KickBans();

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
                Console.WriteLine("Connected to {0}.", Bot.CurrentWorld);
                Bot.Chat += OnChat;
                Bot.WorldDisconnect += onWorldDisconnect;
                Bot.UniverseDisconnect += onUniverseDisconnect;
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
            UserManager.Dispose();
            return;
        }

        static void UpdateLoop()
        {
            Bot.Wait(-1);
            Thread.Sleep(100);
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
            // Accept only commands
            if (!chat.Message.StartsWith("!")) return;

            var intercept = chat.Message
                .Trim()
                .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            var requester = UserManager[chat.Session].Avatar;
            var command = intercept[0].Substring(1).ToLower();
            var data = intercept.Length == 2
                ? intercept[1].Trim()
                : "";

            // Reject bots
            if (requester.IsBot) return;

            switch (command)
            {
                case "telegram":
                    Telegrams.CmdTelegram(chat, data);
                    return;
                case "help":
                case "commands":
                    if (DateTime.Now.Subtract(lastHelp).TotalMinutes < 5) return;
                    Bot.Say("!telegram <who>: <message> , !seed, !mycoords, !petition <who>, !(set)home, !random");
                    Bot.Say("!(add/del)jump <name>, !j(ump) <name>, !back, !(join/invite) <who>, !(x/alt/z) <offset>");
                    lastHelp = DateTime.Now;
                    return;
                case "seed":
                    Bot.GoTo(requester.X, requester.Y, requester.Z);
                    Bot.Say("At your location; right click me to duplicate my avatar into a new object");
                    return;
                case "mycoords":
                    Bot.Say("{0}: {1:f4}, {2:f4}, {3:f4}", requester.Name, requester.X, requester.Y, requester.Z);
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
                case "sethome":
                    UserManager.CmdSetHome(requester);
                    return;
                case "home":
                    UserManager.CmdGoHome(requester);
                    return;
                case "back":
                    UserManager.CmdGoBack(sender, requester, data);
                    return;
                case "crash":
                    if (requester.Name != "Roy Curtis") return;
                    throw new Exception("Forced crash");
                case "join":
                    JoinsInvites.OnRequest(requester.Name, data, false);
                    return;
                case "invite":
                    JoinsInvites.OnRequest(requester.Name, data, true);
                    return;
                case "yes":
                    JoinsInvites.OnResponse(requester.Name, true);
                    return;
                case "no":
                    JoinsInvites.OnResponse(requester.Name, false);
                    return;
                case "petition":
                    KickBans.OnPetition(requester.Name, data);
                    return;
                case "kickvote":
                    KickBans.OnVote(requester.Name, false);
                    return;
                case "banvote":
                    KickBans.OnVote(requester.Name, true);
                    return;
                case "alt":
                    float alt;

                    if (float.TryParse(data, out alt))
                        Bot.Avatars.Teleport(
                            requester.Session,
                            "",
                            new Vector3
                            {
                                X = requester.X,
                                Y = requester.Y + alt,
                                Z = requester.Z
                            },
                            0, 0);
                    return;
                case "x":
                    float x;

                    if (float.TryParse(data, out x))
                        Bot.Avatars.Teleport(
                            requester.Session,
                            "",
                            new Vector3
                            {
                                X = requester.X + x,
                                Y = requester.Y,
                                Z = requester.Z
                            },
                            0, 0);
                    return;
                case "z":
                    float z;

                    if (float.TryParse(data, out z))
                        Bot.Avatars.Teleport(
                            requester.Session,
                            "",
                            new Vector3
                            {
                                X = requester.X,
                                Y = requester.Y,
                                Z = requester.Z + z
                            },
                            0, 0);
                    return;
                case "random":
                    Bot.Avatars.Teleport(
                        requester.Session,
                        "",
                        new Vector3
                        {
                            X = Rand.Next(-32750, 32750),
                            Z = Rand.Next(-32750, 32750)
                        },
                        0, 0);

                    return;
                case "terrain":
                    for (var i = 0; i < 16; i++)
                        for (var j = 0; j < 16; j++)
                        {
                            // Comment line out below to not pump during node sets
                            Bot.Wait(100);
                            Bot.Terrain.SetNode(
                                new TerrainNode
                                {
                                    TileX = 0,
                                    TileZ = 0,
                                    X = i,
                                    Z = j,
                                    Hole = false,
                                    Rotation = TerrainRotation.North,
                                    Texture = 6,
                                    Height = 1.0f
                                });
                        }

                    return;
                case "terrainquery":
                    Bot.Terrain.Query(0, 0, new int[32, 32]);

                    return;
                default:
                    Console.WriteLine("Unknown command: {0}", command);
                    return;
            }
        }
    }
}
