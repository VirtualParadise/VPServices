using System;

namespace VPServices.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Returns an integer amount of seconds difference between DateTime.Now and
        /// this DateTime value
        /// </summary>
        public static long SecondsToNow(this DateTime time)
        {
            return (long)DateTime.Now.Subtract(time).TotalSeconds;
        }

        public static long ToUnixTimestamp(this DateTime time)
        {
            return (long)(time - UnixEpoch).TotalSeconds;
        }
    }
}
