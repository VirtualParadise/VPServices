using System;
using System.IO;
using SQLite;
using VP;

namespace VPServices.Services
{
    partial class Logging : IService
    {
        public void Migrate(VPServices app, int target)
        {
            this.connection = app.Connection;

            switch (target)
            {
                case 1:
                    migSetupSQLite(app);
                    migDatToSQLite(app);
                    break;
            }
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
                #region Build history transaction
                connection.BeginTransaction();
                var lines = File.ReadAllLines(fileBuildHistory);

                foreach ( var line in lines )
                {
                    var parts = line.TerseSplit(',');
                    connection.Insert(new sqlBuildHistory
                    {
                        X    = float.Parse(parts[0]),
                        Y    = float.Parse(parts[1]),
                        Z    = float.Parse(parts[2]),
                        When = long.Parse(parts[3]),
                        ID   = 0,
                        Type = sqlBuildType.Unknown
                    });
                }

                connection.Commit(); 
                #endregion

                var backup = fileBuildHistory + ".bak";
                File.Move(fileBuildHistory, backup);
                Log.Fine(Name, "Migrated .dat build history log to SQLite; backed up to '{0}'", backup);
            }

            if ( File.Exists(fileUserHistory) )
            {
                #region User history transaction
                connection.BeginTransaction();
                var lines = File.ReadAllLines(fileUserHistory);

                foreach ( var line in lines )
                {
                    var parts = line.TerseSplit(',');
                    connection.Insert(new sqlUserHistory
                    {
                        Type =
                            parts[0] == "enter"
                            ? sqlUserType.Enter
                            : sqlUserType.Leave,
                        Name = parts[1],
                        When = long.Parse(parts[2]),
                        ID   = 0
                    });
                }

                connection.Commit(); 
                #endregion

                var backup = fileUserHistory + ".bak";
                File.Move(fileUserHistory, backup);
                Log.Fine(Name, "Migrated .dat user history log to SQLite; backed up to '{0}'", backup);
            }
        }
    }
}
