using Microsoft.Extensions.Configuration;
using SQLite;
using System;
using System.Linq;

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
            var migration = GetMigrationVersion();

            if (migration >= MigrationVersion)
                return;

            foreach (var service in Services)
                for (var i = migration; i < MigrationVersion; i++)
                {
                    service.Migrate(this, i + 1);
                    servicesLogger.Information("Migrated '{Service}' to version {Version}", service.Name, i + 1);
                }

            servicesLogger.Debug("All services migrated to version {Version}", MigrationVersion);
        }
        #endregion

        #region User settings migrations
        /// <summary>
        /// Applies any migrational changes from older to newer versions
        /// </summary>
        void migrateUsers()
        {
            var target = GetMigrationVersion() + 1;

            switch (target)
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

            servicesLogger.Debug("Created SQLite tables for user settings");
        }
        #endregion

        private const string VersionTableName = "Version";
        private bool MigrationVersionTableExists => 
            Connection.ExecuteScalar<string>("SELECT name FROM sqlite_master WHERE type='table' AND name=?;", VersionTableName) == VersionTableName;

        private int GetMigrationVersion()
        {
            int version;
            if (MigrationVersionTableExists)
            {
                version = Connection.ExecuteScalar<int>("SELECT VersionNumber from Version");
            }
            else
            {
                version = CoreSettings.GetValue("Version", 0);
            }
            return version;
        }

        private void SaveMigrationVersion(int version)
        {
            if (MigrationVersionTableExists)
            {
                Connection.Execute("UPDATE Version SET VersionNumber = ?", version);
            }
            else
            {
                Connection.Execute("CREATE TABLE Version (VersionNumber INTEGER)");
                Connection.Execute("INSERT INTO Version VALUES(?)", version);
            }
        }
    }
}
