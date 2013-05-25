using SQLite;
using System;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public SQLiteConnection Connection;

        public void SetupDatabase()
        {
            var database = CoreSettings.Get("Database", "VPServices.db");
            Connection   = new SQLiteConnection(database, true);
            Log.Info("Database", "Set up {0} as database", database);
        }
    }
}
