using System;
using VP;

namespace VPServ.Services
{
    public interface IService : IDisposable
    {
        string Name { get; }
        void Init(VPServ app, Instance bot);
    }
}
