using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VpNet;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        /// <summary>
        /// Global list of currently present users
        /// </summary>
        public List<Avatar<Vector3>> Users = new List<Avatar<Vector3>>();

        #region User getters
        /// <summary>
        /// Gets all known sessions of a given case-insensitive user name
        /// </summary>
        public Avatar<Vector3>[] GetUsers(string name)
        {
            lock (SyncMutex)
            {
                var query = from   u in Users
                            where  u.Name.IEquals(name)
                            select u;

                return query.ToArray();
            }
        }

        /// <summary>
        /// Gets case-insensitive user by name or returns null
        /// </summary>
        public Avatar<Vector3> GetUser(string name)
        {
            return GetUsers(name).FirstOrDefault();
        }

        /// <summary>
        /// Gets user by session number or returns null
        /// </summary>
        public Avatar<Vector3> GetUser(int session)
        {
            lock (SyncMutex)
            {
                var query = from   u in Users
                            where  u.Session == session
                            select u;

                return query.FirstOrDefault();
            }
        } 
        #endregion
    }

    static class AvatarExtensions
    {
        public static VpNet.Dictionary<string, string> GetSettings(this Avatar<Vector3> user)
        {
            lock (VPServices.App.DataMutex)
            {
                var conn  = VPServices.App.Connection;
                var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? ORDER BY Name ASC", user.UserId);
                var dict  = new VpNet.Dictionary<string, string>();

                foreach (var entry in query)
                    dict.Add(entry.Name, entry.Value);

                return dict;
            }
        }

        /// <summary>
        /// Gets a user setting of the specified key as a string, or returns null if
        /// not set
        /// </summary>
        public static string GetSetting(this Avatar<Vector3> user, string key)
        {
            try
            {
                lock (VPServices.App.DataMutex)
                {
                    var conn  = VPServices.App.Connection;
                    var query = conn.Query<sqlUserSettings>("SELECT * FROM UserSettings WHERE UserID = ? AND Name = ? COLLATE NOCASE", user.UserId, key);

                    if (query.Count() <= 0)
                        return null;
                    else
                        return query.First().Value;
                }
            }
            catch (Exception e)
            {
                Log.Severe("Users", "Could not get setting '{0}' for ID {1}", key, user.UserId);
                e.LogFullStackTrace();

                return null;
            }
        }

        public static int GetSettingInt(this Avatar<Vector3> user, string key, int defValue = 0)
        {
            var setting = GetSetting(user, key);
            int value;

            if ( setting == null || !int.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public static bool GetSettingBool(this Avatar<Vector3> user, string key, bool defValue = false)
        {
            var  setting = GetSetting(user, key);
            bool value;

            if ( setting == null || !bool.TryParse(setting, out value) )
                return defValue;
            else
                return value;
        }

        public static DateTime GetSettingDateTime(this Avatar<Vector3> user, string key)
        {
            var      setting = GetSetting(user, key);
            DateTime value;

            if ( setting == null || !DateTime.TryParse(setting, out value) )
                return TDateTime.UnixEpoch;
            else
                return value;
        }

        public static void SetSetting(this Avatar<Vector3> user, string key, object value)
        {
            lock (VPServices.App.DataMutex)
            {
                VPServices.App.Connection.InsertOrReplace(new sqlUserSettings
                {
                    UserID = user.UserId,
                    Name   = key,
                    Value  = value.ToString()
                });
            }
        }

        public static void DeleteSetting(this Avatar<Vector3> user, string key)
        {
            lock (VPServices.App.DataMutex)
                VPServices.App.Connection.Execute("DELETE FROM UserSettings WHERE UserID = ? AND Name = ?", user.UserId, key);
        }
    }

    [Table("UserSettings")]
    class sqlUserSettings
    {
        [Indexed]
        public int    UserID { get; set; }
        [Indexed]
        public string Name   { get; set; }
        [MaxLength(100000)]
        public string Value  { get; set; }
    }

    /// <summary>
    /// Represents an immutable value of 3D Cartesian coordinates and rotations of any
    /// avatar
    /// </summary>
    /// <remarks>
    /// Pulled this out of libVPNET as part of refactoring, source:
    /// https://github.com/VirtualParadise/libVPNET/blob/6c2c8fa70a7397730f0ed3f5f6c52015f99b7a24/libVPNET/Types/AvatarPosition.cs
    /// </remarks>
    public struct AvatarPosition
    {
        /// <summary>
        /// Represents an avatar position at a world's ground zero (zero position and
        /// rotations)
        /// </summary>
        public static readonly AvatarPosition GroundZero = new AvatarPosition();

        #region Native creators
        //internal static AvatarPosition FromSelf(IntPtr pointer)
        //{
        //    return new AvatarPosition
        //    {
        //        X = Functions.vp_float(pointer, FloatAttributes.MyX),
        //        Y = Functions.vp_float(pointer, FloatAttributes.MyY),
        //        Z = Functions.vp_float(pointer, FloatAttributes.MyZ),
        //        Yaw = Functions.vp_float(pointer, FloatAttributes.MyYaw),
        //        Pitch = Functions.vp_float(pointer, FloatAttributes.MyPitch),
        //    };
        //}

        //internal static AvatarPosition FromAvatar(IntPtr pointer)
        //{
        //    return new AvatarPosition
        //    {
        //        X = Functions.vp_float(pointer, FloatAttributes.AvatarX),
        //        Y = Functions.vp_float(pointer, FloatAttributes.AvatarY),
        //        Z = Functions.vp_float(pointer, FloatAttributes.AvatarZ),
        //        Yaw = Functions.vp_float(pointer, FloatAttributes.AvatarYaw),
        //        Pitch = Functions.vp_float(pointer, FloatAttributes.AvatarPitch),
        //    };
        //}

        //internal static AvatarPosition FromTeleport(IntPtr pointer)
        //{
        //    return new AvatarPosition
        //    {
        //        X = Functions.vp_float(pointer, FloatAttributes.TeleportX),
        //        Y = Functions.vp_float(pointer, FloatAttributes.TeleportY),
        //        Z = Functions.vp_float(pointer, FloatAttributes.TeleportZ),
        //        Yaw = Functions.vp_float(pointer, FloatAttributes.TeleportYaw),
        //        Pitch = Functions.vp_float(pointer, FloatAttributes.TeleportPitch),
        //    };
        //}
        #endregion

        #region Public fields
        /// <summary>
        /// Gets the X (east-west) coordinate of this position
        /// </summary>
        public double X;
        /// <summary>
        /// Gets the Y (altitude) coordinate of this position
        /// </summary>
        public double Y;
        /// <summary>
        /// Gets the Z (south-north) coordinate of this position
        /// </summary>
        public double Z;
        /// <summary>
        /// Gets the yaw (left-right) rotation of this position in degrees
        /// </summary>
        public double Yaw;
        /// <summary>
        /// Gets the pitch (down-up) rotation of this position in degrees
        /// </summary>
        public double Pitch;
        #endregion

        #region Public properties
        /// <summary>
        /// Gets a Vector3D value for coordinates
        /// </summary>
        public Vector3 Coordinates
        {
            get { return new Vector3(X, Y, Z); }
        }
        #endregion

        #region Public constructors
        /// <summary>
        /// Creates a new AvatarPosition from a given Vector3D for coordinates and pitch
        /// and yaw values for rotation
        /// </summary>
        /// <param name="pos">Coordinates of position using a Vector3D</param>
        /// <param name="yaw">Yaw (left-right) rotation in degrees</param>
        /// <param name="pitch">Pitch (down-up) rotation in degrees</param>
        public AvatarPosition(Vector3 pos, float yaw, float pitch)
        {
            X = pos.X;
            Y = pos.Y;
            Z = pos.Z;
            Yaw = yaw;
            Pitch = pitch;
        }

        /// <summary>
        /// Creates a new AvatarPosition from a given set of coordinates and pitch and
        /// yaw values for rotation
        /// </summary>
        /// <param name="x">X (east-west) coordinate of position</param>
        /// <param name="y">Y (altitude) coordinate of position</param>
        /// <param name="z">Z (south-north) coordinate of position</param>
        /// <param name="yaw">Yaw (left-right) rotation in degrees</param>
        /// <param name="pitch">Pitch (down-up) rotation in degrees</param>
        public AvatarPosition(float x, float y, float z, float yaw, float pitch)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
        }
        #endregion

        const string format = "X: {0} Y: {1} Z: {2} Yaw: {3}° Pitch: {4}°";
        /// <summary>
        /// Formats this AvatarPosition to a human-readable string
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, format, X, Y, Z, Yaw, Pitch);
        }
    }
}