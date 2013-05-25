using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    public class SwordFight : IService
    {
        const string msgReminder   = "PvP swordfight mode is still enabled for you";
        const string msgToggle     = "PvP swordfight mode has been {0} for {1}";
        const string msgTooSoon    = "It is too soon for you to switch PVP status ({0} seconds left)";
        const string msgPunchbag   = "I am at your location; spam me with clicks to attack";
        const string msgHealth     = "Your health is {0} points";
        const string msgKill       = "Rest in peace, {0}. {1} gains 5 health.";
        const string keyLastSwitch = "SwordFightToggle";
        const string keyHealth     = "SwordFightHealth";
        const string keyDeath      = "SwordFightLastDeath";
        const string keyMode       = "SwordFight";

        VPServices         app;
        List<VPObject> spawned = new List<VPObject>();

        public string Name { get { return "Sword fight"; } }
        public void Init(VPServices app, Instance bot)
        {
            this.app = app;

            app.Commands.AddRange( new[] {
                new Command
                (
                    "Swordfight: Toggle", "^swordfight", cmdTogglePVP,
                    @"Toggles or sets swordfighting (PVP) mode for you",
                    @"!swordfight `[true|false]`"
                ),

                new Command
                (
                    "Swordfight: Punchbag", "^punchbag", cmdPunchbag,
                    @"Brings me to user's location to practise swordfighting",
                    @"!punchbag", 5
                ),

                new Command
                (
                    "Swordfight: Health", "^health", cmdHealth,
                    @"Notifys the user of their health",
                    @"!health"
                )
            });

            app.Bot.Property.CallbackObjectCreate += onCreate;
            app.Bot.Avatars.Clicked               += onClick;
            app.AvatarEnter                       += onEnter;
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        #region Command handlers
        bool cmdTogglePVP(VPServices app, Avatar who, string data)
        {
            var  lastSwitch = who.GetSettingDateTime(keyLastSwitch);
            bool toggle     = false;

            // Reject if too soon
            if ( lastSwitch.SecondsToNow() < 60 )
            {
                var timeLeft = 60 - lastSwitch.SecondsToNow();
                app.Warn(who.Session, msgTooSoon, timeLeft);
                return true;
            }

            if ( data != "" )
            {
                // Try to parse user given boolean; silently ignore on failure
                if ( !VPServices.TryParseBool(data, out toggle) )
                    return false;
            }
            else
                toggle = !who.GetSettingBool(keyMode);

            // Set new boolean, timeout and if new, health
            who.SetSetting(keyMode, toggle);
            who.SetSetting(keyLastSwitch, DateTime.Now);
            initialHealth(who);

            var verb = toggle ? "enabled" : "disabled";
            app.NotifyAll(msgToggle, verb, who.Name);
            return true;
        }

        bool cmdPunchbag(VPServices app, Avatar who, string data)
        {
            app.Bot.GoTo(who.X, who.Y, who.Z);
            app.Notify(who.Session, msgPunchbag);

            return true;
        }

        bool cmdHealth(VPServices app, Avatar who, string data)
        {
            initialHealth(who);
            app.Notify(who.Session, msgHealth, who.GetSettingInt(keyHealth));
            return true;
        } 
        #endregion

        #region Event handlers
        void onClick(Instance bot, AvatarClick click)
        {
            if ( click.TargetSession == 0 )
                return;

            var source = app.GetUser(click.SourceSession);
            var target = app.GetUser(click.TargetSession);

            // Needed as there is no intermediate click event
            if ( source == null )
                return;

            // Null session suggests bot
            if ( target == null )
            {
                hitBot(source);
                return;
            }


            if ( !source.GetSettingBool(keyMode, false) || !target.GetSettingBool(keyMode, false) )
                return;

            var diffX = Math.Abs(source.X - target.X);
            var diffY = Math.Abs(source.Y - target.Y);
            var diffZ = Math.Abs(source.Z - target.Z);

            if ( diffX > .5 || diffY > .4 || diffZ > .5 )
                return;

            if ( cannotHit(source) || cannotHit(target) )
            {
                createHoverText(target.Position, 0, false);
                return;
            }

            var targetHealth = target.GetSettingInt(keyHealth, 100);
            var critical     = VPServices.Rand.Next(100) <= 10;
            var damage       = VPServices.Rand.Next(5, 25) * ( critical ? 3 : 1 );

            createHoverText(target.Position, damage, critical);
            createBloodSplat(target.Position);

            if ( targetHealth - damage <= 0 )
            {
                app.AlertAll(msgKill, target.Name, source.Name);

                target.SetSetting(keyDeath, DateTime.Now);
                target.SetSetting(keyHealth, 100);
                source.SetSetting(keyHealth, source.GetSettingInt(keyHealth) + 5);
                bot.Avatars.Teleport(click.TargetSession, AvatarPosition.GroundZero);
            }
            else
                target.SetSetting(keyHealth, targetHealth - damage);
        }

        void onEnter(Instance sender, Avatar user)
        {
            if ( user.GetSettingBool(keyMode) )
                app.Warn(user.Session, msgReminder);
        } 
        #endregion

        #region Health logic
        void initialHealth(Avatar who)
        {
            if (who.GetSettingInt(keyHealth) <= 0)
                who.SetSetting(keyHealth, 100);
        }
        #endregion

        #region Attack logic
        void hitBot(Avatar source)
        {
            var critical = VPServices.Rand.Next(100) <= 10;
            var damage   = VPServices.Rand.Next(5, 25) * ( critical ? 3 : 1 );

            createHoverText(app.Bot.Position, damage, critical);
            createBloodSplat(app.Bot.Position);
        }

        bool cannotHit(Avatar who)
        {
            if ( who.X < 1 && who.X > -1 )
                if ( who.Z < 1 && who.Z > -1 )
                    return true;

            var death = who.GetSettingDateTime(keyDeath);
            if ( death.SecondsToNow() < 5 )
                return true;

            return false;
        } 
        #endregion

        #region UI logic
        void createHoverText(AvatarPosition pos, int damage, bool critical)
        {
            var offsetX     = ( (float) VPServices.Rand.Next(-100, 100) ) / 2000;
            var offsetZ     = ( (float) VPServices.Rand.Next(-100, 100) ) / 2000;
            var description = string.Format("{0}{1}", 0 - damage, critical ? " !!!" : "");
            var color       = damage == 0 ? "blue" : "red";
            var hover       = new VPObject
            {
                Model = "p:fac100x50,s.rwx",
                Rotation = Quaternion.ZeroEuler,
                Action = string.Format("create sign color={0} bcolor=ffff0000 hmargin=20, ambient 1, move 0 2 time=5 wait=10, solid no", color),
                Description = description,
                Position = new Vector3(pos.X + offsetX, pos.Y + .2f, pos.Z + offsetZ)
            };

            app.Bot.Property.AddObject(hover);
            spawned.Add(hover);
        }

        void createBloodSplat(AvatarPosition pos)
        {
            var offsetX     = ( (float) VPServices.Rand.Next(-100, 100) ) / 2000;
            var offsetY     = ( (float) VPServices.Rand.Next(0, 100) ) / 5000;
            var offsetZ     = ( (float) VPServices.Rand.Next(-100, 100) ) / 2000;
            var size        = VPServices.Rand.Next(80, 200);

            var hover       = new VPObject
            {
                Model = "p:flat" + size + ".rwx",
                Rotation = Quaternion.ZeroEuler,
                Action = "create texture bloodsplat1.png, normalmap nmap-puddle1, specularmap smap-puddle1, specular .6 30, solid no",
                Position = new Vector3(pos.X + offsetX, pos.Y + offsetY, pos.Z + offsetZ)
            };

            app.Bot.Property.AddObject(hover);
            spawned.Add(hover);
        }

        void onCreate(Instance bot, ObjectCallbackData obj)
        {
            if ( spawned.Contains(obj.Object) )
                Task.Factory.StartNew(() =>
                {
                    var timer = DateTime.Now;

                    while ( true )
                    {
                        if ( timer.SecondsToNow() > 3 )
                        {
                            bot.Property.DeleteObject(obj.Object);
                            return;
                        }

                        Thread.Sleep(1000);
                    }
                });
        } 
        #endregion
    }
}
