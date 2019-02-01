using System;
using System.IO;
using VPServices.Extensions;

namespace VPServices.Services
{
    public partial class Todo : IService
    {
        public void Migrate(VPServices app, int target)
        {
            this.connection = app.Connection;

            switch (target)
            {
                case 3:
                    migSetupSQLite(app);
                    migDatToSQLite(app);
                    break;
            }
        }

        void migSetupSQLite(VPServices app)
        {
            connection.CreateTable<sqlTodo>();
            logger.Debug("Created SQLite table for todo list");
        }

        void migDatToSQLite(VPServices app)
        {
            var fileIdeas = "Ideas.dat";
            string[] lines;
            string   backup;

            if ( !File.Exists(fileIdeas) )
                return;

            connection.BeginTransaction();
            lines = File.ReadAllLines(fileIdeas);

            foreach ( var line in lines )
            {
                var parts = line.TerseSplit(',');
                connection.Insert(new sqlTodo
                {
                    WhoID = 0,
                    Who   = parts[0],
                    What  = parts[1].Replace("%COMMA%", ","),
                    When  = DateTime.Parse(parts[2]),
                    Done  = bool.Parse(parts[3])
                });
            }

            connection.Commit();
            
            backup = fileIdeas + ".bak";
            File.Move(fileIdeas, backup);
            logger.Debug("Migrated .dat ideas list to SQLite; backed up to '{BackupFile}'", backup);
        }
    }
}
