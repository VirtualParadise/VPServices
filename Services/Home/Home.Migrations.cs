using System;

namespace VPServices.Services
{
    public partial class Home : IService
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
