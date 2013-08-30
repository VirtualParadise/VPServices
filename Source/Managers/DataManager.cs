using SQLite;
using System;

namespace VPServices
{
    public class DataManager
    {
        const string tag = "Database";

        public SQLiteConnection SQL;

        public DataManager()
        {
            var db  = VPServices.Settings.Core.Get("Database", "VPServices.db");
                SQL = new SQLiteConnection(db, true);

            Log.Info(tag, "Set up '{0}' as database", db);
        }

        public void Takedown()
        {
            if (SQL != null)
                SQL.Dispose();
        }
    }
}
