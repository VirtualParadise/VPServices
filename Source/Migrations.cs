using Nini.Config;
using System;
using System.IO;

namespace VPServices
{
    /// <summary>
    /// Handles global Services migrations for services on startup
    /// </summary>
    public static class Migrations
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
            lock (VPServices.Data)
                migrateUsers();
        }

        
        #endregion

        #region User settings migrations
        /// <summary>
        /// Applies any migrational changes from older to newer versions
        /// </summary>
        void migrateUsers()
        {
            var target = CoreSettings.GetInt("Version", 0) + 1;

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
