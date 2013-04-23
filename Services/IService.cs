using System;
using VP;

namespace VPServices.Services
{
    public interface IService : IDisposable
    {
        string Name { get; }
        void Init(VPServices app, Instance bot);
    }
}
