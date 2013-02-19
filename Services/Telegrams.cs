using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using VP;

namespace VPServ.Services
{
    class Telegrams : IService
    {
        const string FILE_TELEGRAMS = "Telegrams.dat";

        public List<Telegram> storedTelegrams = new List<Telegram>();

        public string Name { get { return "Telegrams"; } }

        public void Init(VPServ app, Instance bot)
        {
            // Load all saved telegrams
            if (File.Exists(FILE_TELEGRAMS))
                foreach (var tgram in File.ReadAllLines(FILE_TELEGRAMS))
                    storedTelegrams.Add(new Telegram(tgram));
            
            VPServ.Instance.Commands.Add(
                new Command("Telegram", "^(telegram|tg(ram)?|compose)$", cmdTelegram,
                    @"Composes a telegram to a target user in the format: `!telegram *who*: *message*`")
            );

            bot.Chat += checkTelegrams;
        }

        public void Dispose()
        {
            saveTelegrams();
        }

        void saveTelegrams()
        {
            Log.Debug(Name, "Saving to file");    
            File.WriteAllLines(FILE_TELEGRAMS,
                from t in storedTelegrams
                select t.ToString(), Encoding.UTF8);
        }

        void cmdTelegram(VPServ serv, Avatar who, string data)
        {
            var matches = Regex.Match(data, "^(.+?): (.+)$");
            if (!matches.Success) return;
            
            var target = matches.Groups[1].Value.Trim();
            var msg = matches.Groups[2].Value.Trim();

            storedTelegrams.Add(new Telegram { From = who.Name, To = target, Message = msg });
            saveTelegrams();

            serv.Bot.Say("{0}: Saved for {1}", who.Name, target);
            Log.Info(Name, "Recorded from {0} for {1}", who.Name, target);       
        }

        void checkTelegrams(Instance bot, Chat chat)
        {
            var user = VPServ.Instance.GetUser(chat.Session);
            var settings = VPServ.Instance.GetUserSettings(user);
            if (user.IsBot) return;

            bool sentOne = false;
            foreach (var tg in storedTelegrams)
                if (!tg.Sent && chat.Name.ToLower() == tg.To.ToLower())
                {
                    Thread.Sleep(500); //TEMP: fix for VP crash
                    bot.Say("{0}: You have a telegram from {1}: {2}", chat.Name, tg.From, tg.Message);
                    tg.Sent = true;
                    sentOne = true;
                }

            if (sentOne)
            {
                // Remove all sent telegrams and save
                storedTelegrams.RemoveAll((t) => { return t.Sent; });
                saveTelegrams();
            }
        }
    }

    class Telegram
    {
        public string To;
        public string From;
        public string Message;
        public bool Sent;

        public Telegram() { }

        /// <summary>
        /// Creates telegram from comma seperated value string
        /// </summary>
        public Telegram(string csv)
        {
            if (csv == "" || !csv.Contains(","))
                throw new ArgumentNullException();

            var parts = csv.Split(new[] { "," }, StringSplitOptions.None);
            To = parts[0];
            From = parts[1];
            Message = parts[2].Replace("%COMMA", ",");
        }

        /// <summary>
        /// Formats telegram data into a CSV string
        /// </summary>
        public override string ToString()
        {
            return string.Join(",", To, From, Message.Replace(",", "%COMMA"));
        }
    }
}
