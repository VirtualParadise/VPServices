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
        /// Loads all services that implement the ServiceBase interface into Loaded
        /// </summary>
        public void LoadServices()
        {
            //http://stackoverflow.com/questions/699852/how-to-find-all-the-classes-which-implement-a-given-interface
            var type     = typeof(IService);
            var services = from   t in Assembly.GetExecutingAssembly().GetTypes()
                           where  t.GetInterfaces().Contains(type) && !t.IsInterface
                           select Activator.CreateInstance(t) as IService;

            Services.AddRange(services);
        }

        public void InitServices()
        {
            foreach (var service in Services)
            {
                service.Load(this, Bot);
                Log.Fine("Services", "Loaded service '{0}'", service.Name);
            }
        }
    }
}
