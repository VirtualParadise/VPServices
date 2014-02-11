using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPServices.Services;

namespace VPServices
{
    public delegate void ServiceArgs(IService service);

    public class ServiceManager
    {
        const string tag = "Services";

        public event ServiceArgs Loaded;
        public event ServiceArgs Unloaded;

        List<IService> services = new List<IService>();

        public void Setup()
        {
            // http://stackoverflow.com/questions/699852/how-to-find-all-the-classes-which-implement-a-given-interface
            var type      = typeof(IService);
            var available = from   t in Assembly.GetExecutingAssembly().GetTypes()
                            where  t.GetInterfaces().Contains(type) && !t.IsInterface
                            select Activator.CreateInstance(t) as IService;

            foreach (var service in available)
            {
                var config  = GetSettings(service);
                var enabled = bool.Parse(config["Enabled"] ?? "true");

                if (!enabled)
                {
                    Log.Debug(tag, "Skipping load of service '{0}' as it is disabled in the config", service.Name);
                    continue;
                }

                service.Load();
                services.Add(service);
                Log.Fine(tag, "Loaded service '{0}'", service.Name);

                if (Loaded != null)
                    Loaded(service);
            }

            Log.Debug(tag, "{0} services discovered and loaded", services.Count);
        }

        public void Takedown()
        {
            foreach (var service in services)
            {
                service.Unload();
                Log.Fine(tag, "Unloaded service '{0}'", service.Name);

                if (Unloaded != null)
                    Unloaded(service);
            }

            services.Clear();
            Log.Info(tag, "All services unloaded");
        }

        public IService[] GetAll()
        {
            return services.ToArray();
        }

        public KeyDataCollection GetSettings(IService service)
        {
            var configName = "Service." + service.Name;
            VPServices.Settings.Ini.Sections.AddSection(configName);

            return VPServices.Settings.Ini[configName];
        }
    }
}
