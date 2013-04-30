using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPServices.Services;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        /// <summary>
        /// Latest version for migration
        /// </summary>
        const int Version = 1;

        /// <summary>
        /// Global list of all loaded services
        /// </summary>
        public List<IService> Services = new List<IService>();

        /// <summary>
        /// Disposes all loaded services and clears list
        /// </summary>
        public void ClearServices()
        {
            foreach (var plugin in Services)
                plugin.Dispose();

            Services.Clear();
        }

        /// <summary>
        /// Loads all services that implement the IService interface into Loaded
        /// </summary>
        public void LoadServices()
        {
            //http://stackoverflow.com/questions/699852/how-to-find-all-the-classes-which-implement-a-given-interface
            var type             = typeof(IService);
            var internalServices =
                from   t in Assembly.GetExecutingAssembly().GetTypes()
                where  t.GetInterfaces().Contains(type)
                       && !t.IsInterface
                select Activator.CreateInstance(t) as IService;

            Services.AddRange(internalServices);
            migrateServices();
            initServices();
        }

        void initServices()
        {
            foreach (var service in Services)
            {
                service.Init(this, Bot);
                Log.Fine("Services", "Loaded service '{0}'", service.Name);
            }
        }

        void migrateServices()
        {
            var migration = CoreSettings.GetInt("Version", 0);

            if ( migration >= Version )
                return;

            foreach (var service in Services)
                for (var i = migration; i < Version; i++)
                {
                    service.Migrate(this, i + 1);
                    Log.Fine("Services", "Migrated '{0}' to version {1}", i + 1);
                }

            CoreSettings.Set("Version", Version);
            Log.Debug("Services", "All services migrated to version {0}", Version);
        }
    }
}
