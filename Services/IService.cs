using System;
using VpNet;

namespace VPServices.Services
{
    public interface IService : IDisposable
    {
        string Name { get; }

        void Init(VPServices app, VirtualParadiseClient bot);
        void Migrate(VPServices app, int target);
    }
}
