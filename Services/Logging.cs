using System;
using System.IO;
using VP;

namespace VPServ.Services
{
    /// <summary>
    /// Logs user and build events to file
    /// </summary>
    class Logging : IService
    {
        /// <summary>
        /// Build log stream
        /// </summary>
        StreamWriter buildStream = new StreamWriter("BuildHist.dat", true) { AutoFlush = true };

        /// <summary>
        /// User entry/exit log stream
        /// </summary>
        StreamWriter userStream = new StreamWriter("UserHist.dat", true) { AutoFlush = true };

        public string Name { get { return "Logging"; } }

        public void Init(VPServ app, Instance bot)
        {
            bot.Property.ObjectCreate += onObjChange;
            bot.Property.ObjectChange += onObjChange;
            bot.Avatars.Enter += onAvatarEnter;
            bot.Avatars.Leave += onAvatarLeave;
        }

        public void Dispose()
        {
            buildStream.Flush();
            buildStream.Close();
            userStream.Flush();
            userStream.Close();
        }

        void onObjChange(Instance sender, int sessionId, VPObject o)
        {
            buildStream.WriteLine("{0},{1},{2},{3}",
                Math.Round(o.Position.X, 3),
                Math.Round(o.Position.Y, 2),
                Math.Round(o.Position.Z, 3),
                (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
        }

        void onAvatarEnter(Instance sender, Avatar avatar)
        {
            userStream.WriteLine("enter,{0},{1}",
                avatar.Name,
                (int) DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
        }

        public void onAvatarLeave(Instance sender, Avatar avatar)
        {
            // Write to log
            userStream.WriteLine("leave,{0},{1}",
                avatar.Name,
                (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
        }
    }
}
