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
        List<IService> loaded   = new List<IService>();

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

                services.Add(service);

                if (!enabled)
                {
                    Log.Warn(tag, "Skipping load of service '{0}' as it is disabled in the config", service.Name);
                    continue;
                }
                
                load(service);
            }

            Log.Debug(tag, "{0} services discovered, {1} loaded", services.Count, loaded.Count);
        }

        public void Takedown()
        {
            foreach ( var service in loaded.ToArray() )
                unload(service);

            loaded.Clear();
            services.Clear();
            Log.Info(tag, "All services unloaded");
        }

        public bool Load(string name)
        {
            var service = Get(name);

            if (service == null)
            {
                Log.Warn(tag, "Tried to load non-existant service '{0}'", name);
                return false;
            }
            
            if ( loaded.Contains(service) )
                return false;

            return load(service);
        }

        bool load(IService service)
        {
            service.Load();
            loaded.Add(service);
            Log.Fine(tag, "Loaded service '{0}'", service.Name);
                
            if (Loaded != null)
                Loaded(service);

            return true;
        }

        public bool Unload(string name)
        {
            var service = GetLoaded(name);

            if (service == null)
                return false;

            return unload(service);
        }

        bool unload(IService service)
        {
            service.Unload();
            loaded.Remove(service);
            Log.Fine(tag, "Unloaded service '{0}'", service.Name);

            if (Unloaded != null)
                Unloaded(service);

            return true;
        }

        public IService Get(string name)
        {
            return services.FirstOrDefault( s => s.Name.IEquals(name) );
        }

        public IService GetLoaded(string name)
        {
            return loaded.FirstOrDefault( s => s.Name.IEquals(name) );
        }

        public IService[] GetAll()
        {
            return services.ToArray();
        }

        public IService[] GetAllLoaded()
        {
            return loaded.ToArray();
        }

        public KeyDataCollection GetSettings(IService service)
        {
            var configName = "Service." + service.Name;
            VPServices.Settings.Ini.Sections.AddSection(configName);

            return VPServices.Settings.Ini[configName];
        }
    }
}
