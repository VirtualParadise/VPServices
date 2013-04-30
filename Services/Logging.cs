using System;
using System.IO;
using SQLite;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Logs user and build events to file
    /// </summary>
    class Logging : IService
    {
        SQLiteConnection connection;

        public string Name { get { return "Logging"; } }
        public void Init(VPServices app, Instance bot)
        {
            bot.Property.ObjectCreate += (s,i,o) => { onObjChange(s,i,o, sqlBuildType.Create); };
            bot.Property.ObjectChange += (s,i,o) => { onObjChange(s,i,o, sqlBuildType.Modify); };
            bot.Property.ObjectDelete += onObjDelete;
            bot.Avatars.Enter         += onAvatarEnter;
            bot.Avatars.Leave         += onAvatarLeave;

            this.connection = app.Connection;
        }

        public void Migrate(VPServices app, int target)
        {
            switch (target)
            {
                case 1:
                    migSetupSQLite(app);
                    migDatToSQLite(app);
                    break;
            }
        }

        public void Dispose()
        {
        }

        //TODO: unix epoch constant
        void onObjChange(Instance sender, int sessionId, VPObject o, sqlBuildType type)
        {
            connection.Insert( new sqlBuildHistory
            {
                ID   = o.Id,
                X    = o.Position.X,
                Y    = o.Position.Y,
                Z    = o.Position.Z,
                Type = type,
                When = (int) TDateTime.UnixTicks
            });
        }

        void onObjDelete(Instance sender, int sessionId, int objectId)
        {
            connection.Insert( new sqlBuildHistory
            {
                ID   = objectId,
                X    = 0,
                Y    = 0,
                Z    = 0,
                Type = sqlBuildType.Delete,
                When = (int) TDateTime.UnixTicks
            });
        }

        void onAvatarEnter(Instance sender, Avatar avatar)
        {
            connection.Insert ( new sqlUserHistory
            {
                ID = avatar.
            });
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

        void migSetupSQLite(VPServices app)
        {
            connection.CreateTable<sqlBuildHistory>();
            connection.CreateTable<sqlUserHistory>();
            Log.Fine(Name, "Created SQLite tables for build and user history");
        }

        void migDatToSQLite(VPServices app)
        {
            var fileBuildHistory = "BuildHist.dat";
            var fileUserHistory  = "UserHist.dat";

            if ( File.Exists(fileBuildHistory) )
            {
                connection.BeginTransaction();
                var lines = File.ReadAllLines(fileBuildHistory);
                
                foreach (var line in lines)
                {
                    var parts = line.TerseSplit(',');
                    connection.Insert(new sqlBuildHistory
                    {
                        X    = float.Parse(parts[0]),
                        Y    = float.Parse(parts[1]),
                        Z    = float.Parse(parts[2]),
                        When = int.Parse(parts[3]),
                        ID   = 0,
                        Type = sqlBuildType.Unknown
                    });
                }

                connection.Commit();
                lines = null;
                File.Move(fileBuildHistory, fileBuildHistory + ".bak");
                Log.Fine(Name, "Migrated .dat build history log to SQLite");
            }

            if ( File.Exists(fileUserHistory) )
            {
                connection.BeginTransaction();
                var lines = File.ReadAllLines(fileUserHistory);
                
                foreach (var line in lines)
                {
                    var parts = line.TerseSplit(',');
                    connection.Insert(new sqlUserHistory
                    {
                        Type =
                            parts[0] == "enter"
                            ? sqlUserType.Enter
                            : sqlUserType.Leave,
                        Name = parts[1],
                        When = int.Parse(parts[2]),
                        ID   = 0
                    });
                }

                connection.Commit();
                lines = null;
                File.Move(fileUserHistory, fileUserHistory + ".bak");
                Log.Fine(Name, "Migrated .dat user history log to SQLite");
            }
        }
    }

    class sqlBuildHistory
    {
        [Indexed]
        public int          ID   { get; set; }
        public float        X    { get; set; }
        public float        Y    { get; set; }
        public float        Z    { get; set; }
        public int          When { get; set; }
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

    class sqlUserHistory
    {
        [Indexed]
        public int         ID   { get; set; }
        public string      Name { get; set; }
        public int         When { get; set; }
        public sqlUserType Type { get; set; }
    }

    enum sqlUserType
    {
        Enter = 0,
        Leave
    }
}
