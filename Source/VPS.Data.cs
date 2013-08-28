using SQLite;
using System;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public SQLiteConnection Connection;

        /// <summary>
        /// Mutex to lock to when accessing collections or objects shared across tasks
        /// </summary>
        public object SyncMutex = new object();

        /// <summary>
        /// Mutex to lock to when accessing databases (SQLite methods, etc)
        /// </summary>
        public object DataMutex = new object();

        public void SetupDatabase()
        {
            var database = CoreSettings.Get("Database", "VPServices.db");
            Connection   = new SQLiteConnection(database, true);
            Connection.BusyTimeout = TimeSpan.MaxValue;

            Log.Info("Database", "Set up {0} as database", database);
        }

        public void CloseDatabase()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }
    }
}
