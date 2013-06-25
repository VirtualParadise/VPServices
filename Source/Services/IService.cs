using System;
using VP;

namespace VPServices.Services
{
    public interface IService
    {
        /// <summary>
        /// Returns the canonical name of the service
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the service's migration level
        /// </summary>
        int MigrationLevel { get; }

        /// <summary>
        /// Called when this service is loaded
        /// </summary>
        /// <param name="app">VPServices state for context</param>
        void Load(VPServices app);

        /// <summary>
        /// Called when this service is unloaded. Should dispose any resources used and
        /// unregister any events from VPServices
        /// </summary>
        /// <param name="app">VPServices state for context</param>
        void Unload(VPServices app);

        void Migrate(VPServices app, int target);
    }
}
