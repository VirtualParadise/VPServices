using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPServices.Services;

namespace VPServices
{
    public class ServiceManager
    {
        const string tag = "Services";

        List<IService> services = new List<IService>();

        public T GetService<T>()
            where T : class, IService
        {
            var type = typeof(T);

            return (T) services.FirstOrDefault( s => s.GetType().Equals(type) );
        }

        public void Setup()
        {
            //http://stackoverflow.com/questions/699852/how-to-find-all-the-classes-which-implement-a-given-interface
            var type      = typeof(IService);
            var available = from   t in Assembly.GetExecutingAssembly().GetTypes()
                            where  t.GetInterfaces().Contains(type) && !t.IsInterface
                            select Activator.CreateInstance(t) as IService;

            foreach (var service in available)
            {
                var enabled = VPServices.Settings.Plugins.GetBoolean(service.Name, true);

                if (!enabled)
                {
                    Log.Debug(tag, "Skipping load of service '{0}' as it is disabled in the config", service.Name);
                    continue;
                }

                service.Load();
                services.Add(service);
                Log.Fine(tag, "Loaded service '{0}'", service.Name);
            }

            Log.Debug(tag, "{0} services discovered and loaded", services.Count);
        }

        public void Takedown()
        {
            foreach (var service in services)
            {
                service.Unload();
                Log.Fine(tag, "Unloaded service '{0}'", service.Name);
            }

            services.Clear();
            Log.Debug(tag, "All services unloaded");
        }
    }
}
