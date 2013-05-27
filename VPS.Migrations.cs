using Nini.Config;
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
            migrateUsers();
            migrateServices();
        }

        /// <summary>
        /// Iterates through all services and invokes any migrations they contain
        /// </summary>
        void migrateServices()
        {
            var migration = CoreSettings.GetInt("Version", 0);

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
            var target = CoreSettings.GetInt("Version", 0) + 1;

            switch ( target )
            {
                case 1:
                    migSetupUserSQLite();
                    migUserIniToSQLite();
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

        void migUserIniToSQLite()
        {
            var userIni = "UserSettings.ini";

            if ( !File.Exists(userIni) )
                return;

            var configSource = new IniConfigSource(userIni);
            foreach ( IConfig config in configSource.Configs )
            {
                // Discard bot settings
                if ( config.Name.StartsWith("__") )
                {
                    Log.Fine("Migration", "Found config for bot '{0}'; discarding", config.Name);
                    continue;
                }

                var keys = config.GetKeys();
                if ( keys.Length <= 0 )
                {
                    Log.Fine("Migration", "Found empty config for '{0}'; discarding", config.Name);
                    continue;
                }

            promptUserID:
                Console.WriteLine("\n[Migrations] What Virtual Paradise account number is user '{0}'?", config.Name);
                Console.Write("> ");
                var givenId = Console.ReadLine();
                int id;

                if ( !int.TryParse(givenId, out id) )
                    goto promptUserID;

                Connection.BeginTransaction();
                foreach ( var key in keys )
                {
                    Log.Fine("Users", "Migrating config key '{0}' for user '{1}'", key, id);
                    Connection.Insert(new sqlUserSettings
                    {
                        UserID = id,
                        Name = key,
                        Value = config.Get(key)
                    });
                }
                Connection.Commit();
            }

            var backup = userIni + ".bak";
            File.Move(userIni, backup);
            Log.Debug("Users", "Migrated INI user settings to SQLite; backed up to '{0}'", backup);
        }  
        #endregion
    }
}
