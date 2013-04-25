using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VP;

namespace VPServices.Services
{
    class Telegrams : IService
    {
        const  string defaultFileTelegrams = "Telegrams.dat";
        const  string msgTelegrams         = "You have {0} telegram(s); say !read to read";
        const  string msgTelegram          = "Sent by {0} on {1}:";
        const  string msgNoTelegrams       = "You have no telegrams to read";
        const  string msgTelegramSent      = "Your telegram to {0} has been sent";

        public List<Telegram> storedTelegrams = new List<Telegram>();

        public string Name { get { return "Telegrams"; } }

        public void Init(VPServices app, Instance bot)
        {
            // Load all saved telegrams
            if (File.Exists(defaultFileTelegrams))
                foreach (var tgram in File.ReadAllLines(defaultFileTelegrams))
                    storedTelegrams.Add(new Telegram(tgram));
            
            VPServices.App.Commands.AddRange(new[] {
                new Command
                (
                    "Telegrams: Compose", "^(telegram|tg(ram)?|compose)$", cmdSendTelegram,
                    @"Composes a telegram to a user",
                    @"!tg `user: message`"
                ),

                new Command
                (
                    "Telegrams: check", "^(telegrams|read)$", cmdReadTelegrams,
                    @"Gets all pending telegrams",
                    @"!read"
                ),
            });

            bot.Chat          += (b,c) => { checkTelegrams(b, c.Session, c.Name); };
            bot.Avatars.Enter += (b,c) => { checkTelegrams(b, c.Session, c.Name); };
            bot.Avatars.Leave += onLeave;
        }

        public void Dispose()
        {
            saveTelegrams();
        }

        #region Command handler
        bool cmdSendTelegram(VPServices app, Avatar who, string data)
        {
            var matches = Regex.Match(data, "^(.+?): (.+)$");
            if ( !matches.Success )
                return false;

            var target = matches.Groups[1].Value.Trim();
            var msg    = matches.Groups[2].Value.Trim();
            var gram   = new Telegram
            {
                From = who.Name,
                To = target,
                Message = msg
            };

            storedTelegrams.Add(gram);
            saveTelegrams();

            app.Notify(who.Session, msgTelegramSent, target);
            return Log.Info(Name, "Recorded from {0} for {1}", who.Name, target);
        }

        bool cmdReadTelegrams(VPServices app, Avatar who, string data)
        {
            var  grams   = from tg in storedTelegrams
                           where tg.To.IEquals(who.Name)
                           select tg;

            if ( grams.Count() <= 0 )
            {
                app.Warn(who.Session, msgNoTelegrams);
                return true;
            }

            foreach ( var gram in grams )
            {
                app.Bot.ConsoleMessage(who.Session, ChatEffect.Bold, VPServices.ColorAlert, "", msgTelegram, gram.From, gram.When);
                app.Bot.ConsoleMessage(who.Session, ChatEffect.None, VPServices.ColorAlert, "", gram.Message);
                app.Bot.ConsoleMessage(who.Session, ChatEffect.None, VPServices.ColorAlert, "", "");
            }

            storedTelegrams.RemoveAll((tg) => { return tg.To.IEquals(who.Name); });
            saveTelegrams();
            return true;
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Marks all telegrams of a leaving avatar as "unaware", so tha they can again
        /// be reminded on re-entry
        /// </summary>
        void onLeave(Instance sender, Avatar avatar)
        {
            foreach ( var tg in storedTelegrams )
                if ( tg.To.IEquals(avatar.Name) )
                    tg.Aware = false;
        } 
        #endregion

        #region Telegram data logic
        void saveTelegrams()
        {
            Log.Debug(Name, "Saving to file");
            File.WriteAllLines(defaultFileTelegrams,
                from t in storedTelegrams
                select t.ToString(), Encoding.UTF8);
        }

        void checkTelegrams(Instance bot, int session, string name)
        {
            var grams = from tg in storedTelegrams
                        where !tg.Aware && tg.To.IEquals(name)
                        select tg;

            if ( grams.Count() > 0 )
            {
                VPServices.App.Alert(session, msgTelegrams, grams.Count());
                foreach ( var tg in grams )
                    tg.Aware = true;
            }
        } 
        #endregion        
    }

    class Telegram
    {
        public string   To;
        public string   From;
        public string   Message;
        public DateTime When = DateTime.Now;
        public bool     Aware;

        public Telegram() { }

        /// <summary>
        /// Creates telegram from comma seperated value string
        /// </summary>
        public Telegram(string csv)
        {
            if (csv == "" || !csv.Contains(","))
                throw new ArgumentNullException();

            var parts = csv.Split(new[] { "," }, StringSplitOptions.None);
            To      = parts[0];
            From    = parts[1];
            Message = parts[2].Replace("%COMMA", ",");

            // Backwards compat
            if (parts.Length == 4)
                When = DateTime.Parse(parts[3]);
            else
                When = DateTime.MinValue;
        }

        /// <summary>
        /// Formats telegram data into a CSV string
        /// </summary>
        public override string ToString()
        {
            return string.Join(",", To, From, Message.Replace(",", "%COMMA"), When);
        }
    }
}
