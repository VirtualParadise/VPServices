using System;
using VpNet;

namespace VPServices.Services
{
    public interface IService : IDisposable
    {
        string Name { get; }

        void Init(VPServices app, Instance bot);
        void Migrate(VPServices app, int target);
    }
}
