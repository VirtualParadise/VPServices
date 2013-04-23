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
        /// Loads all services that implement the IService interface into Loaded
        /// </summary>
        public void LoadServices()
        {
            //http://stackoverflow.com/questions/699852/how-to-find-all-the-classes-which-implement-a-given-interface
            var internalPlugins =
                from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.GetInterfaces().Contains(typeof(IService))
                    && t.GetConstructor(Type.EmptyTypes) != null
                select Activator.CreateInstance(t) as IService;

            foreach (var plugin in internalPlugins)
            {
                Services.Add(plugin);
                plugin.Init(this, Bot);
                Log.Fine("Services", "Loaded service {0}", plugin.Name);
            }
        }
    }
}
