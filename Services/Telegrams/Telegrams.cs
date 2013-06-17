using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VP;

namespace VPServices.Services
{
    partial class Telegrams : IService
    {
        public string Name { get { return "Telegrams"; } }

        public void Init(VPServices app, Instance bot)
        {
            VPServices.App.Commands.AddRange(new[] {
                new Command
                (
                    "Telegrams: Compose", "^(telegram|tg(ram)?|compose)", cmdSendTelegram,
                    @"Composes a telegram to a user",
                    @"!tg `user: message`"
                ),

                new Command
                (
                    "Telegrams: Check", "^(telegrams|read)", cmdReadTelegrams,
                    @"Gets all pending telegrams",
                    @"!read"
                ),
            });

            app.Chat        += (b,a,c) => { checkTelegrams(b, a.Session, a.Name); };
            app.AvatarEnter += (b,c)   => { checkTelegrams(b, c.Session, c.Name); };
            app.AvatarLeave += onLeave;
            this.connection  = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgTelegrams    = "You have {0} telegram(s); say !read to read";
        const string msgTelegram     = "Sent by {0} on {1}:";
        const string msgNoTelegrams  = "You have no telegrams to read";
        const string msgTelegramSent = "Your telegram to {0} has been sent";

        Dictionary<string, bool> told = new Dictionary<string, bool>();
        SQLiteConnection         connection; 
        #endregion
        
        #region Command handlers
        bool cmdSendTelegram(VPServices app, Avatar who, string data)
        {
            var matches = Regex.Match(data, "^(.+?): (.+)$");
            if ( !matches.Success )
                return false;

            var target = matches.Groups[1].Value.Trim();
            var msg    = matches.Groups[2].Value.Trim();
            
            connection.Insert(new sqlTelegram
            {
                Source  = who.Name,
                Target  = target,
                Message = msg,
                When    = DateTime.Now,
                Read    = false
            });

            told[target.ToLower()] = false;
            app.Notify(who.Session, msgTelegramSent, target);
            return Log.Info(Name, "Recorded from {0} for {1}", who.Name, target);
        }

        bool cmdReadTelegrams(VPServices app, Avatar who, string data)
        {
            var grams = getUnread(who.Name);

            if ( grams.Count() <= 0 )
            {
                app.Warn(who.Session, msgNoTelegrams);
                return true;
            }

            connection.BeginTransaction();
            foreach ( var gram in grams )
            {
                app.Bot.ConsoleMessage(who.Session, ChatEffect.Bold, VPServices.ColorAlert, "", msgTelegram, gram.Source, gram.When);
                app.Bot.ConsoleMessage(who.Session, ChatEffect.None, VPServices.ColorAlert, "", gram.Message);
                app.Bot.ConsoleMessage(who.Session, ChatEffect.None, VPServices.ColorAlert, "", "");
                gram.Read = true;
                connection.Update(gram);
            }

            connection.Commit();            
            return true;
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Marks all telegrams of a leaving avatar as "unaware", so tha they can again
        /// be reminded on re-entry
        /// </summary>
        void onLeave(Instance sender, Avatar who)
        {
            var grams = getUnread(who.Name);

            if ( grams.Count() > 0 && VPServices.App.GetUser(who.Name) == null )
                told[who.Name.ToLower()] = false;
        } 
        #endregion

        #region Telegram data logic
        List<sqlTelegram> getUnread(string target)
        {
            var query = connection.Query<sqlTelegram>("SELECT * FROM Telegrams WHERE Read = ? AND Target = ? COLLATE NOCASE", false, target);
            return query;
        }

        void checkTelegrams(Instance bot, int session, string name)
        {
            var safeName = name.ToLower();
            var grams    = getUnread(name);
            var count    = grams.Count();
            var isTold   = told.ContainsKey(safeName) ? told[safeName] : false;

            if ( count > 0 && !isTold )
            {
                told[safeName] = true;
                VPServices.App.Alert(session, msgTelegrams, count);
            }
        } 
        #endregion        
    }

    [Table("Telegrams")]
    class sqlTelegram
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        /// <summary>
        /// Comma separated list of targets
        /// </summary>
        [Indexed]
        public string   Target  { get; set; }
        public string   Source  { get; set; }
        [MaxLength(255)]
        public string   Message { get; set; }
        public DateTime When    { get; set; }
        public bool     Read    { get; set; }
    }
}
