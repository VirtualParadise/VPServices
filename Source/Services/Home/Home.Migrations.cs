using Nini.Config;
using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    public partial class Home : ServiceBase
    {
        public void Migrate(VPServices app, int target)
        {
            this.connection = app.Connection;

            switch (target)
            {
                case 2:
                    migSetupSQLite(app);
                    break;
            }
        }

        void migSetupSQLite(VPServices app)
        {
            connection.CreateTable<sqlHome>();
            Log.Debug(Name, "Created SQLite table for home locations");
        }
    }
}
