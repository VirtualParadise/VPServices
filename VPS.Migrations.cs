using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        /// <summary>Latest version for migration</summary>
        /// 
        /// <remarks>
        /// 0 - Pre-SQLite Services (initial setup)
        /// 1 - Jumps, Logging, Telegrams, UserSettings services
        /// 2 - Home service
        /// 3 - Todo, Facts services
        /// </remarks>
        const int MigrationVersion = 3;

        #region VPServices migrations
        public void PerformMigrations()
        {
            lock (VPServices.App.DataMutex)
            {
                migrateUsers();
                migrateServices();
            }
        }

        /// <summary>
        /// Iterates through all services and invokes any migrations they contain
        /// </summary>
        void migrateServices()
        {
            var migration = CoreSettings.GetValue("Version", 0);

            if ( migration >= MigrationVersion )
                return;

            foreach ( var service in Services )
                for ( var i = migration; i < MigrationVersion; i++ )
                {
                    service.Migrate(this, i + 1);
                    Log.Fine("Services", "Migrated '{0}' to version {1}", service.Name, i + 1);
                }

            Log.Debug("Services", "All services migrated to version {0}", MigrationVersion);
        } 
        #endregion

        #region User settings migrations
        /// <summary>
        /// Applies any migrational changes from older to newer versions
        /// </summary>
        void migrateUsers()
        {
            var target = CoreSettings.GetValue("Version", 0) + 1;

            switch ( target )
            {
                case 1:
                    migSetupUserSQLite();
                    break;
            }
        }

        void migSetupUserSQLite()
        {
            Connection.CreateTable<sqlUserSettings>();
            Connection.Execute(
                @"CREATE TRIGGER IF NOT EXISTS DeleteOldUserSetting
                BEFORE INSERT ON UserSettings 
                FOR EACH ROW 
                BEGIN 
                DELETE FROM UserSettings WHERE UserID = new.UserID AND Name = new.Name;
                END");

            Log.Debug("Users", "Created SQLite tables for user settings");
        }
        #endregion
    }
}
