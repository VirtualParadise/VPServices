using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VpNet;
using VpNet.Extensions;
using VpNet.Interfaces;
using VpNet.ManagedApi.Scene;

namespace VPServices.Extensions
{
    public static class CompassExtensions
    {
        public static (string Direction, double Angle) ToCompassTuple<TAvatar>(this TAvatar avatar) where TAvatar : IAvatar<Vector3>
        {
            var angle = (avatar.Rotation.Y % 360 + 360) % 360;

            var direction = VpNet.Extensions.CompassExtensions.ToCompassLongString(avatar);

            return (Direction: direction, Angle: angle);
        }
    }
}
