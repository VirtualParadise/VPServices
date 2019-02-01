using System;
using Serilog;
using SQLite;
using VpNet;
using VPServices.Extensions;

namespace VPServices.Services
{
    /// <summary>
    /// Logs user and build events to file
    /// </summary>
    /// <remarks>
    /// Loggers use UNIX timestamps instead of DateTime values for future convinience
    /// with logger tools, which are more likely to use timestamps.
    /// </remarks>
    partial class Logging : IService
    {
        readonly ILogger logger = Log.ForContext("Tag", "Logging");
        public string Name
        { 
            get { return "Logging"; }
        }

        public void Init(VPServices app, Instance bot)
        {
            bot.OnObjectCreate += (sender, args) => { objectEvent(args.VpObject, sqlBuildType.Create); };
            bot.OnObjectChange += (sender, args) => { objectEvent(args.VpObject, sqlBuildType.Modify); };
            bot.OnObjectDelete += onObjDelete;
            app.AvatarEnter += (s,a) => { userEvent(a, sqlUserType.Enter); };
            app.AvatarLeave += (s,a) => { userEvent(a, sqlUserType.Leave); };

            this.connection = app.Connection;
        }

        public void Dispose() { }

        SQLiteConnection connection;

        void objectEvent(VpObject<Vector3> o, sqlBuildType type)
        {
            lock (VPServices.App.DataMutex)
                connection.Insert( new sqlBuildHistory
                {
                    ID   = o.Id,
                    X    = (float)o.Position.X,
                    Y    = (float)o.Position.Y,
                    Z    = (float)o.Position.Z,
                    Type = type,
                    When = DateTime.UtcNow.ToUnixTimestamp()
                });
        }

        void onObjDelete(Instance sender, ObjectDeleteArgsT<Avatar<Vector3>, VpObject<Vector3>, Vector3> args)
        {
            lock (VPServices.App.DataMutex)
                connection.Insert( new sqlBuildHistory
                {
                    ID   = args.VpObject.Id,
                    X    = 0,
                    Y    = 0,
                    Z    = 0,
                    Type = sqlBuildType.Delete,
                    When = DateTime.UtcNow.ToUnixTimestamp()
                });
        }

        void userEvent(Avatar<Vector3> avatar, sqlUserType type)
        {
            if ( VPServices.App.LastConnect.SecondsToNow() < 10 )
                return;

            lock (VPServices.App.DataMutex)
                connection.Insert ( new sqlUserHistory
                {
                    ID   = avatar.UserId,
                    Name = avatar.Name,
                    Type = type,
                    When = DateTime.UtcNow.ToUnixTimestamp()
                });
        }        
    }

    [Table("BuildHistory")]
    class sqlBuildHistory
    {
        public int          ID   { get; set; }
        public float        X    { get; set; }
        public float        Y    { get; set; }
        public float        Z    { get; set; }
        public long         When { get; set; }
        public sqlBuildType Type { get; set; }
    }

    enum sqlBuildType
    {
        // For version 0
        Unknown = 0,
        Create,
        Delete,
        Modify
    }

    [Table("UserHistory")]
    class sqlUserHistory
    {
        public int         ID   { get; set; }
        public string      Name { get; set; }
        public long        When { get; set; }
        public sqlUserType Type { get; set; }
    }

    enum sqlUserType
    {
        Enter = 0,
        Leave
    }
}
