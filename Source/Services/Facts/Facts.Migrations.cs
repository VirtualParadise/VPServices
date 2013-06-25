using System;
using System.IO;

namespace VPServices.Services
{
    public partial class Facts : IService
    {
        public void Migrate(VPServices app, int target)
        {
            this.connection = app.Connection;

            switch (target)
            {
                case 3:
                    migSetupSQLite(app);
                    break;
            }
        }

        void migSetupSQLite(VPServices app)
        {
            connection.CreateTable<sqlFact>();
            Log.Debug(Name, "Created SQLite table for facts");
        }
    }
}
