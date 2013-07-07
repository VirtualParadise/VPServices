using System;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    public interface IService
    {
        #region Properties
        /// <summary>
        /// Returns the canonical name of the service
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the service's migration level
        /// </summary>
        int MigrationLevel { get; } 
        #endregion

        #region Abstract methods
        /// <summary>
        /// Called when Services is loading this service. Should initialize any resources
        /// and register all commands and events
        /// </summary>
        /// <param name="app">VPServices state for context</param>
        void Load(VPServices app);

        /// <summary>
        /// Called when Service is unloaded. Should dispose any resources used and
        /// unregister any events from VPServices
        /// </summary>
        /// <param name="app">VPServices state for context</param>
        void Unload(VPServices app);

        /// <summary>
        /// Called if Services wants this service to migrate to a higher level, based
        /// on the services' given migration level
        /// </summary>
        /// <remarks>
        /// Migrations should set up any tables or data sources initially and then make
        /// dataset changes per level, if needed
        /// </remarks>
        /// <param name="app">VPServices state for context</param>
        /// <param name="target">Target level to migrate to</param>
        void Migrate(VPServices app, int target);  
        #endregion
    }
}
