using System;
using System.Globalization;
using System.IO;

namespace VPServices.Services
{
    public partial class Jumps : ServiceBase
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
            connection.CreateTable<sqlJump>();
            Log.Debug(Name, "Created SQLite table for jump points");
        }

        void migDatToSQLite(VPServices app)
        {
            var fileJumps = "Jumps.dat";
            string[] lines;
            string   backup;

            if ( !File.Exists(fileJumps) )
                return;

            connection.BeginTransaction();
            lines = File.ReadAllLines(fileJumps);

            foreach ( var line in lines )
            {
                var parts = line.TerseSplit(',');
                connection.Insert(new sqlJump
                {
                    Name    = parts[0],
			        X       = float.Parse(parts[1], CultureInfo.InvariantCulture),
			        Y       = float.Parse(parts[2], CultureInfo.InvariantCulture),
			        Z       = float.Parse(parts[3], CultureInfo.InvariantCulture),
			        Yaw     = float.Parse(parts[4], CultureInfo.InvariantCulture),
			        Pitch   = float.Parse(parts[5], CultureInfo.InvariantCulture),
                    When    = TDateTime.UnixEpoch,
                    Creator = "Unknown"
                });
            }

            connection.Commit();
            
            backup = fileJumps + ".bak";
            File.Move(fileJumps, backup);
            Log.Debug(Name, "Migrated .dat jump list to SQLite; backed up to '{0}'", backup);
        }
    }
}
