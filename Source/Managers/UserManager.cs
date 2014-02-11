using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPServices.Internal
{
    public delegate void UserArgs(User user);
    public delegate void UserLeaveArgs(User user, bool disconnection);

    public class UserManager
    {
        const string tag = "Users";

        public event UserArgs      Enter;
        public event UserLeaveArgs Leave;
        public event UserArgs      Change;

        List<User> users = new List<User>();

        public void Setup()
        {
            VPServices.Worlds.Added += w => {
                w.Bot.Avatars.Enter  += onAvatarEnter;
                w.Bot.Avatars.Leave  += onAvatarLeave;
                w.Bot.Avatars.Change += onAvatarChange;

                w.Bot.UniverseDisconnect += onDisconnect;
                w.Bot.WorldDisconnect    += onDisconnect;
            };

            VPServices.Worlds.Removed += w => {
                w.Bot.Avatars.Enter  -= onAvatarEnter;
                w.Bot.Avatars.Leave  -= onAvatarLeave;
                w.Bot.Avatars.Change -= onAvatarChange;

                w.Bot.UniverseDisconnect -= onDisconnect;
                w.Bot.WorldDisconnect    -= onDisconnect;

                removeByWorld(w);
                Log.Fine(tag, "Cleared all known users of world '{0}' due to removal", w);
            };

            var sql = VPServices.Data.SQL.CreateTable<sqlUserSettings>();
            VPServices.Data.SQL.Execute(
                @"CREATE TRIGGER IF NOT EXISTS DeleteOldUserSetting
                BEFORE INSERT ON UserSettings 
                FOR EACH ROW 
                BEGIN 
                DELETE FROM UserSettings WHERE UserID = new.UserID AND Name = new.Name;
                END");

            if (sql != 0)
                Log.Info(tag, "Created SQLite tables for user settings");

            Log.Debug(tag, "Set up user-related world events and SQL");
        }

        public void Takedown()
        {
            if (Leave != null)
                foreach (var user in users)
                    Leave(user, false);

            Enter  = null;
            Leave  = null;
            Change = null;

            users.Clear();
            Log.Info(tag, "All users cleared");
        }

        public User[] GetAll()
        {
            return users.ToArray();
        }

        public User[] ByName(string name)
        {
            return users.Where( u => u.Name.IEquals(name) ).ToArray();
        }

        public User BySession(int session)
        {
            return users.FirstOrDefault( u => u.Session == session );
        }

        void removeByWorld(World world)
        {
            var affected = users.Where( u => u.World.Equals(world) ).ToArray();

            foreach (var user in affected)
            {
                users.Remove(user);

                if (Leave != null)
                    Leave(user, true);
            }
        }

        void onAvatarEnter(Instance bot, Avatar avatar)
        {
            var world = VPServices.Worlds.Get(bot);

            if (world.State != WorldState.Connected)
                return;

            var user  = new User(avatar, world);
            users.Add(user);

            Log.Debug(tag, "User '{0}' has entered world '{1}'", user, world);
            if (Enter != null)
                Enter(user);
        }

        void onAvatarLeave(Instance bot, string name, int session)
        {
            var world = VPServices.Worlds.Get(bot);
            var user  = BySession(session);

            if (user == null)
                return;

            users.Remove(user);

            Log.Debug(tag, "User '{0}' has left world '{1}'", user, world);
            if (Leave != null)
                Leave(user, false);
        }

        void onAvatarChange(Instance bot, Avatar avatar)
        {
            var world = VPServices.Worlds.Get(bot);
            var user  = BySession(avatar.Session);

            if (world.State != WorldState.Connected || user == null)
                return;

            user.Avatar.Position = avatar.Position;

            if (Change != null)
                Change(user);
        }

        void onDisconnect(Instance sender, int error)
        {
            var world = VPServices.Worlds.Get(sender);
            
            removeByWorld(world);
            Log.Fine(tag, "Cleared all known users of world '{0}' due to disconnect", world);
        }
    }
}