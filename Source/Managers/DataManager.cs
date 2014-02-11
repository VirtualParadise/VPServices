using SQLite;
using System;

namespace VPServices
{
    public class DataManager
    {
        const string tag = "Database";

        public SQLiteConnection SQL;

        public void Setup()
        {
            var db  = VPServices.Settings.Core["Database"] ?? "VPServices.db";
                SQL = new SQLiteConnection(db, true);

            Log.Info(tag, "Using database '{0}'", db);
        }

        public void Takedown()
        {
            if (SQL != null)
                SQL.Dispose();
        }
    }
}
