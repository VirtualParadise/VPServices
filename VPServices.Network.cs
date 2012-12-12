using System;
using VP.Core;

namespace VPServices
{
    partial class VPServices
    {
        static short ConnAttempts;

        static void ConnectToUniverse()
        {
            ConnAttempts = 0;

            while (ConnAttempts < 10)
            {
                try
                {
                    Console.WriteLine("Connecting to universe...");
                    Bot.Connect();
                    Bot.Login(userName, password, "Services");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed: {0}", e.Message);
                    ConnAttempts++;
                }
            }

            throw new Exception("Could not connect to uniserver after ten attempts.");
        }

        static void Bot_EventUniverseDisconnect(Instance sender)
        {
            Console.WriteLine("Disconnected from universe! Reconnecting...");
            ConnectToUniverse();
        }

        static void Bot_EventWorldDisconnect(Instance sender)
        {
            Console.WriteLine("Disconnected from world! Reconnecting...");
            ConnectToUniverse();
        }
    }
}
