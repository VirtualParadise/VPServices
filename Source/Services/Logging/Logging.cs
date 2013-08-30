using System;
using System.IO;
using SQLite;
using VP;

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
        public string Name
        { 
            get { return "Logging"; }
        }

        public void Load(VPServices app, World bot)
        {
            bot.Property.ObjectCreate += (s,i,o) => { objectEvent(o, sqlBuildType.Create); };
            bot.Property.ObjectChange += (s,i,o) => { objectEvent(o, sqlBuildType.Modify); };
            bot.Property.ObjectDelete += onObjDelete;
            app.AvatarEnter           += (s,a) => { userEvent(a, sqlUserType.Enter); };
            app.AvatarLeave           += (s,a) => { userEvent(a, sqlUserType.Leave); };

            this.connection = app.Connection;
        }

        public void Dispose() { }

        SQLiteConnection connection;

        void objectEvent(VPObject o, sqlBuildType type)
        {
            lock (VPServices.App.DataMutex)
                connection.Insert( new sqlBuildHistory
                {
                    ID   = o.Id,
                    X    = o.Position.X,
                    Y    = o.Position.Y,
                    Z    = o.Position.Z,
                    Type = type,
                    When = TDateTime.UnixTimestamp
                });
        }

        void onObjDelete(World sender, int sessionId, int objectId)
        {
            lock (VPServices.App.DataMutex)
                connection.Insert( new sqlBuildHistory
                {
                    ID   = objectId,
                    X    = 0,
                    Y    = 0,
                    Z    = 0,
                    Type = sqlBuildType.Delete,
                    When = TDateTime.UnixTimestamp
                });
        }

        void userEvent(Avatar avatar, sqlUserType type)
        {
            if ( VPServices.App.LastConnect.SecondsToNow() < 10 )
                return;

            lock (VPServices.App.DataMutex)
                connection.Insert ( new sqlUserHistory
                {
                    ID   = avatar.Id,
                    Name = avatar.Name,
                    Type = type,
                    When = TDateTime.UnixTimestamp
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
