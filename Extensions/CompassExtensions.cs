using VpNet;

namespace VPServices.Extensions
{
	public static class CompassExtensions
    {
        public static (string Direction, double Angle) ToCompassTuple(this Avatar avatar)
        {
            var angle = (avatar.Location.Rotation.Y % 360 + 360) % 360;

            var direction = VpNet.Extensions.CompassExtensions.ToCompassLongString(avatar);

            return (Direction: direction, Angle: angle);
        }
    }
}
