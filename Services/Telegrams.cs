using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    class Telegram
    {
        public string To;
        public string From;
        public string Message;
        public bool Sent;

        public static Telegram FromString(string dat)
        {
            if (dat == "" || !dat.Contains(","))
                return null;

            var parts = dat.Split(new[] { "," }, StringSplitOptions.None);
            return new Telegram
            {
                To = parts[0],
                From = parts[1],
                Message = parts[2].Replace("%COMMA", ","),
                Sent = bool.Parse(parts[3]),
            };
        }

        public string Save()
        {
            return string.Join(",",
                To,
                From,
                Message.Replace(",", "%COMMA"),
                Sent);
        }
    }

    class Telegrams
    {
        const string TGRAMS = "Telegrams.dat";
        const string SETTING_BLOCKED = "BlockingGrams";

        List<Telegram> StoredGrams = new List<Telegram>();

        public Telegrams()
        {
            // Load all saved telegrams
            if (File.Exists(TGRAMS))
                foreach (var tgram in File.ReadAllLines(TGRAMS))
                    StoredGrams.Add(Telegram.FromString(tgram));
            
            VPServices.Bot.Chat += checkTelegrams;
        }

        public void SaveTelegrams()
        {
            File.WriteAllLines(TGRAMS,
                from t in StoredGrams
                select t.Save()
                , Encoding.UTF8);
        }

        public void CmdTelegram(Chat chat, string data)
        {
            var matches = Regex.Match(data, "^(.+?): (.+?)$", RegexOptions.IgnoreCase);
            if (!matches.Success) return;
            
            var target = matches.Groups[1].Value.Trim();
            var msg = matches.Groups[2].Value.Trim();

            StoredGrams.Add(new Telegram { From = chat.Name, To = target, Message = msg });
            VPServices.Bot.Say("{0}: Saved for {1}", chat.Name, target);
            Console.WriteLine("Telegram by {0} recorded for {1}.", chat.Name, target);
            SaveTelegrams();
        }

        void checkTelegrams(Instance bot, Chat chat)
        {
            var user = VPServices.UserManager[chat.Name];
            if (user.Avatar.IsBot) return;

            if (user.Settings.GetBoolean(SETTING_BLOCKED, false)) return;

            bool sentOne = false;
            foreach (var tg in StoredGrams)
                if (!tg.Sent && chat.Name.ToLower() == tg.To.ToLower())
                {
                    bot.Say("{0}: You have a telegram from {1}: {2}", chat.Name, tg.From, tg.Message);
                    tg.Sent = true;
                    sentOne = true;
                    SaveTelegrams();
                }

            if (sentOne) bot.Say("(Say '!telegram <recepient>: <message>' to respond)");
        }
    }
}
