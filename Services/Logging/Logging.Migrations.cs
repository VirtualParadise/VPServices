using System;
using System.IO;

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
            logger.Debug("Created SQLite tables for build and user history");
        }

        void migDatToSQLite(VPServices app)
        {
            var fileBuildHistory = "BuildHist.dat";
            var fileUserHistory  = "UserHist.dat";
            string[] lines;
            string   backup;

            if ( !File.Exists(fileBuildHistory) )
                goto userHistory;
            
            #region Build history transaction
            connection.BeginTransaction();
            lines = File.ReadAllLines(fileBuildHistory);

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

            backup = fileBuildHistory + ".bak";
            File.Move(fileBuildHistory, backup);
            logger.Debug("Migrated .dat build history log to SQLite; backed up to '{BackupFile}'", backup);

        userHistory:
            if ( !File.Exists(fileUserHistory) )
                return;

            #region User history transaction
            connection.BeginTransaction();
            lines = File.ReadAllLines(fileUserHistory);

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

            backup = fileUserHistory + ".bak";
            File.Move(fileUserHistory, backup);
            logger.Debug("Migrated .dat user history log to SQLite; backed up to '{BackupFile}'", backup);
            
        }
    }
}
