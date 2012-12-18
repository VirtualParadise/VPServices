using System;
using VP;

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
                    Bot.Login(userName, password, "Services");
                    VPServices.StartUpTime = DateTime.Now;
                    Bot.Enter(world);
                    Bot.GoTo(0,10,0);
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

        static void onUniverseDisconnect(Instance sender)
        {
            Console.WriteLine("Disconnected from universe! Reconnecting...");
            ConnectToUniverse();
        }

        static void onWorldDisconnect(Instance sender)
        {
            Console.WriteLine("Disconnected from world! Reconnecting...");
            ConnectToUniverse();
        }
    }
}
