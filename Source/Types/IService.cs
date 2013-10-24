namespace VPServices.Services
{
    public interface IService
    {
        /// <summary>
        /// Returns the canonical name of the service
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Called when Services is loading this service. Should initialize any resources
        /// and register all commands and events
        /// </summary>
        void Load();

        /// <summary>
        /// Called when Service is unloaded. Should dispose any resources used and
        /// unregister any events from VPServices
        /// </summary>
        void Unload();
    }
}
